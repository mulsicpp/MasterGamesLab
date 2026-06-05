using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Map.Hoverables
{
    public class HoverableIdPickerFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public Material overrideMaterial;
            public bool flipY = true; // Set to true for Direct3D/Vulkan/Metal

            [Header("Debug")] public bool showDebugOnScreen = false;
            public Material debugBlitMaterial; // Assign Mat_TileIdDebug here!
        }

        public Settings settings = new Settings();
        private TileIdPickerPass pickPass;

        public override void Create()
        {
            pickPass = new TileIdPickerPass(settings)
            {
                // Execute after Opaques to reuse the active Depth Buffer.
                // This prevents picking tiles that are hidden behind other objects!
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // Enqueue if we click OR if debug mode is forcing it to render continuously
            var shouldRender = HoverablePicker.Instance.RequestPending || settings.showDebugOnScreen;

            if (shouldRender && (renderingData.cameraData.cameraType == CameraType.Game ||
                                 renderingData.cameraData.cameraType == CameraType.SceneView))
            {
                renderer.EnqueuePass(pickPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            HoverablePicker.Instance.Cleanup();
        }
    }

    public class TileIdPickerPass : ScriptableRenderPass
    {
        private HoverableIdPickerFeature.Settings settings;

        private ShaderTagId[] shaderTagIds =
            { new ShaderTagId("UniversalForward"), new ShaderTagId("SRPDefaultUnlit") };

        public TileIdPickerPass(HoverableIdPickerFeature.Settings settings)
        {
            this.settings = settings;
        }

        private class RenderPassData
        {
            public TextureHandle idColor;
            public RendererListHandle rendererList;
        }

        private class CopyPassData
        {
            public TextureHandle src;
            public TextureHandle dst;
            public int srcX;
            public int srcY;
        }

        private class BlitPassData
        {
            public TextureHandle src;
            public Material mat;
        } // New for Debug

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Capture the state, then consume it immediately
            bool isPickRequested = HoverablePicker.Instance.RequestPending;
            HoverablePicker.Instance.ConsumeRequest();

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            // --- 1. RENDER IDS (Runs if clicking OR debugging) ---
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_UInt;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            TextureHandle idColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "TileIDColor", false);

            RenderTextureDescriptor depthDesc = cameraData.cameraTargetDescriptor;
            depthDesc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
            depthDesc.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat;
            depthDesc.depthBufferBits = 32;
            depthDesc.msaaSamples = 1;
            TextureHandle idDepth =
                UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, "TileIDDepth", false);

            var sortFlags = SortingCriteria.CommonOpaque;
            var drawSettings = new DrawingSettings(shaderTagIds[0],
                new SortingSettings(cameraData.camera) { criteria = sortFlags });
            drawSettings.SetShaderPassName(1, shaderTagIds[1]);
            if (settings.overrideMaterial != null) drawSettings.overrideMaterial = settings.overrideMaterial;

            var filterSettings = new FilteringSettings(RenderQueueRange.opaque, HoverablePicker.Instance.LayerMask);
            RendererListHandle rendererList =
                renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawSettings,
                    filterSettings));

            using (var builder = renderGraph.AddRasterRenderPass<RenderPassData>("TileIdRender", out var passData))
            {
                passData.idColor = idColor;
                passData.rendererList = rendererList;
                builder.SetRenderAttachment(idColor, 0);
                builder.SetRenderAttachmentDepth(idDepth, AccessFlags.Write);
                builder.UseRendererList(rendererList);

                builder.SetRenderFunc((RenderPassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(true, true, Color.clear);
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }

            // --- 2. EXTRACT PIXEL (ONLY runs when specifically clicking/requesting) ---
            if (isPickRequested && HoverablePicker.Instance.Persistent1X1RT != null)
            {
                TextureHandle persistentDst = renderGraph.ImportTexture(HoverablePicker.Instance.Persistent1X1RT);

                // 1. Handle URP Render Scale by finding the ratio between the texture size and the screen size
                float scaleX = (float)desc.width / cameraData.camera.pixelWidth;
                float scaleY = (float)desc.height / cameraData.camera.pixelHeight;

                int targetX = Mathf.RoundToInt(HoverablePicker.Instance.MousePosition.x * scaleX);
                int targetY = Mathf.RoundToInt(HoverablePicker.Instance.MousePosition.y * scaleY);

                // 2. Handle Y flipping for certain Graphics APIs (Direct3D/Vulkan/Metal)
                if (settings.flipY && SystemInfo.graphicsUVStartsAtTop)
                {
                    targetY = desc.height - 1 - targetY;
                }

                // 3. CRITICAL: Strictly clamp coordinates so resizing the window or 
                // moving the mouse to another monitor never causes a GPU out-of-bounds crash!
                targetX = Mathf.Clamp(targetX, 0, desc.width - 1);
                targetY = Mathf.Clamp(targetY, 0, desc.height - 1);

                using (var builder = renderGraph.AddUnsafePass<CopyPassData>("TileIdCopyPixel", out var copyData))
                {
                    copyData.src = idColor;
                    copyData.dst = persistentDst;
                    copyData.srcX = targetX;
                    copyData.srcY = targetY;

                    builder.UseTexture(idColor, AccessFlags.Read);
                    builder.UseTexture(persistentDst, AccessFlags.Write);

                    builder.SetRenderFunc((CopyPassData data, UnsafeGraphContext context) =>
                    {
                        CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                        // Now strictly guaranteed to fit inside the source texture
                        cmd.CopyTexture(data.src, 0, 0, data.srcX, data.srcY, 1, 1, data.dst, 0, 0, 0, 0);

                        cmd.RequestAsyncReadback(data.dst, HoverablePicker.Instance.Callback);
                    });
                }
            }

            // --- 3. DEBUG BLIT TO SCREEN (ONLY runs if debug toggle is true) ---
            if (settings.showDebugOnScreen && settings.debugBlitMaterial != null)
            {
                TextureHandle cameraColor = resourceData.activeColorTexture;
                if (cameraColor.IsValid())
                {
                    using (var builder =
                           renderGraph.AddRasterRenderPass<BlitPassData>("TileIdDebugBlit", out var blitData))
                    {
                        blitData.src = idColor;
                        blitData.mat = settings.debugBlitMaterial;

                        builder.UseTexture(idColor, AccessFlags.Read);
                        builder.SetRenderAttachment(cameraColor, 0); // Blit straight to the Game View!

                        builder.SetRenderFunc((BlitPassData data, RasterGraphContext context) =>
                        {
                            // Bind our uint texture to the debug material manually
                            data.mat.SetTexture("_TileIdTexture", data.src);
                            // Blitter.BlitTexture automatically binds the source texture to _BlitTexture
                            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.mat, 0);
                        });
                    }
                }
            }
        }
    }
}
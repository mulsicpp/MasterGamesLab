namespace Map.OutlineEffect
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;
    using UnityEngine.Rendering.RenderGraphModule;
    using UnityEngine.Experimental.Rendering;

    public class OutlineDataFeature : ScriptableRendererFeature
    {
        private const string OUTLINE_COLOR_NAME = "_GlobalOutlineColor";
        private const string INNER_COLOR_NAME = "_GlobalOutlineInnerColor";
        private const string OUTLINE_TEXTURE_IDX_NAME = "_GlobalOutlineTextureIdx";
        private const string OUTLINE_DEPTH_NAME = "_GlobalOutlineDepth";

        private class OutlineDataPass : ScriptableRenderPass
        {
            private readonly Material overrideMat;
            private readonly LayerMask layerMask;

            // 1. Declare persistent RTHandles for our render targets
            private RTHandle m_OutlineColor;
            private RTHandle m_InnerColor;
            private RTHandle m_TextureIdx;
            private RTHandle m_Depth;

            private class PassData
            {
                public RendererListHandle RendererList;
            }

            public OutlineDataPass(Material mat, LayerMask layer)
            {
                overrideMat = mat;
                layerMask = layer;
            }

            // 2. Safely release RTHandles to avoid memory leaks
            public void Dispose()
            {
                m_OutlineColor?.Release();
                m_InnerColor?.Release();
                m_TextureIdx?.Release();
                m_Depth?.Release();
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (overrideMat == null) return;

                var cameraData = frameData.Get<UniversalCameraData>();
                var renderingData = frameData.Get<UniversalRenderingData>();

                // 3. Setup descriptors based on the current camera (handles dynamic resolution/resizing)
                var colorDesc = cameraData.cameraTargetDescriptor;
                colorDesc.depthBufferBits = 0; // No depth for color targets
                colorDesc.msaaSamples = 1;
                colorDesc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;

                // Allocate/Reallocate the physical memory outside of the transient Render Graph pool
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_OutlineColor, colorDesc, name: OUTLINE_COLOR_NAME);
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_InnerColor, colorDesc, name: INNER_COLOR_NAME);
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_TextureIdx, colorDesc, name: OUTLINE_TEXTURE_IDX_NAME);

                var depthDesc = cameraData.cameraTargetDescriptor;
                depthDesc.colorFormat = RenderTextureFormat.Depth;
                depthDesc.depthBufferBits = 32;
                depthDesc.msaaSamples = 1;

                RenderingUtils.ReAllocateHandleIfNeeded(ref m_Depth, depthDesc, name: OUTLINE_DEPTH_NAME);

                // 4. Import the RTHandles into the Render Graph so it can use them as TextureHandles
                TextureHandle rt1 = renderGraph.ImportTexture(m_OutlineColor);
                TextureHandle rt2 = renderGraph.ImportTexture(m_InnerColor);
                TextureHandle rt3 = renderGraph.ImportTexture(m_TextureIdx);
                TextureHandle depthRt = renderGraph.ImportTexture(m_Depth);

                using var builder = renderGraph.AddRasterRenderPass<PassData>("Outline Data Pass", out var passData);

                builder.SetRenderAttachment(rt1, 0);
                builder.SetRenderAttachment(rt2, 1);
                builder.SetRenderAttachment(rt3, 2);
                builder.SetRenderAttachmentDepth(depthRt);

                // 5. Expose our safe, persistent textures to global shaders
                builder.SetGlobalTextureAfterPass(rt1, Shader.PropertyToID(OUTLINE_COLOR_NAME));
                builder.SetGlobalTextureAfterPass(rt2, Shader.PropertyToID(INNER_COLOR_NAME));
                builder.SetGlobalTextureAfterPass(rt3, Shader.PropertyToID(OUTLINE_TEXTURE_IDX_NAME));
                builder.SetGlobalTextureAfterPass(depthRt, Shader.PropertyToID(OUTLINE_DEPTH_NAME));

                var sortingSettings = new SortingSettings(cameraData.camera)
                    { criteria = SortingCriteria.CommonOpaque };
                var drawingSettings = new DrawingSettings(new ShaderTagId("UniversalForward"), sortingSettings)
                {
                    overrideMaterial = overrideMat
                };
                var filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);

                var rlParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
                passData.RendererList = renderGraph.CreateRendererList(rlParams);
                builder.UseRendererList(passData.RendererList);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(true, true, Color.clear);
                    context.cmd.DrawRendererList(data.RendererList);
                });
            }
        }

        public Material overrideMaterial;
        public LayerMask outlineLayer;
        private OutlineDataPass pass;

        public override void Create()
        {
            pass = new OutlineDataPass(overrideMaterial, outlineLayer)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (overrideMaterial != null)
            {
                renderer.EnqueuePass(pass);
            }
        }

        // 6. Dispose of the pass when the Feature is destroyed
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pass?.Dispose();
            }
        }
    }
}
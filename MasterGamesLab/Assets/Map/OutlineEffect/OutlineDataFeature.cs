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
            private readonly Material expandMaterialAllChannels;
            private readonly Material expandMaterialDepth; // <--- New
            private readonly LayerMask layerMask;

            private RTHandle m_OutlineColor;
            private RTHandle m_InnerColor;
            private RTHandle m_TextureIdx;
            private RTHandle m_Depth;

            private class PassData
            {
                public RendererListHandle RendererList;
            }

            private class BlurPassData
            {
                public TextureHandle srcTexture;
                public Material material;
                public int passIndex;
            }

            public OutlineDataPass(Material mat, Material expandMaterialAllChannels, Material expandMaterialDepth,
                LayerMask layer)
            {
                this.overrideMat = mat;
                this.expandMaterialAllChannels = expandMaterialAllChannels;
                this.expandMaterialDepth = expandMaterialDepth;
                this.layerMask = layer;
            }

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

                var colorDesc = cameraData.cameraTargetDescriptor;
                colorDesc.depthBufferBits = 0;
                colorDesc.msaaSamples = 1;
                colorDesc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;

                RenderingUtils.ReAllocateHandleIfNeeded(ref m_OutlineColor, colorDesc, name: OUTLINE_COLOR_NAME);
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_InnerColor, colorDesc, name: INNER_COLOR_NAME);
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_TextureIdx, colorDesc, name: OUTLINE_TEXTURE_IDX_NAME);

                var depthDesc = cameraData.cameraTargetDescriptor;
                depthDesc.colorFormat = RenderTextureFormat.Depth;
                depthDesc.depthBufferBits = 32;
                depthDesc.msaaSamples = 1;

                RenderingUtils.ReAllocateHandleIfNeeded(ref m_Depth, depthDesc, name: OUTLINE_DEPTH_NAME);

                TextureHandle rt1 = renderGraph.ImportTexture(m_OutlineColor);
                TextureHandle rt2 = renderGraph.ImportTexture(m_InnerColor);
                TextureHandle rt3 = renderGraph.ImportTexture(m_TextureIdx);
                TextureHandle depthRt = renderGraph.ImportTexture(m_Depth);

                bool doExpandAll = expandMaterialAllChannels != null;
                bool doExpandDepth = expandMaterialDepth != null;

                // --- 1. Main Draw Pass ---
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Outline Data Pass", out var passData))
                {
                    builder.SetRenderAttachment(rt1, 0);
                    builder.SetRenderAttachment(rt2, 1);
                    builder.SetRenderAttachment(rt3, 2);
                    builder.SetRenderAttachmentDepth(depthRt);

                    if (!doExpandAll)
                        builder.SetGlobalTextureAfterPass(rt1, Shader.PropertyToID(OUTLINE_COLOR_NAME));

                    builder.SetGlobalTextureAfterPass(rt2, Shader.PropertyToID(INNER_COLOR_NAME));
                    builder.SetGlobalTextureAfterPass(rt3, Shader.PropertyToID(OUTLINE_TEXTURE_IDX_NAME));

                    if (!doExpandDepth)
                        builder.SetGlobalTextureAfterPass(depthRt, Shader.PropertyToID(OUTLINE_DEPTH_NAME));

                    var sortingSettings = new SortingSettings(cameraData.camera)
                        { criteria = SortingCriteria.CommonOpaque };
                    var drawingSettings = new DrawingSettings(new ShaderTagId("UniversalForward"), sortingSettings)
                        { overrideMaterial = overrideMat };
                    var filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);

                    var rlParams =
                        new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
                    passData.RendererList = renderGraph.CreateRendererList(rlParams);
                    builder.UseRendererList(passData.RendererList);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        context.cmd.ClearRenderTarget(true, true, Color.clear);
                        context.cmd.DrawRendererList(data.RendererList);
                    });
                }

                // --- 3. Expand All Channels Pass (OutlineColor / rt1) ---
                if (doExpandAll)
                {
                    TextureDesc tempDescAll = new TextureDesc(colorDesc.width, colorDesc.height);
                    tempDescAll.colorFormat = colorDesc.graphicsFormat;
                    tempDescAll.depthBufferBits = 0;
                    tempDescAll.msaaSamples = MSAASamples.None;
                    tempDescAll.name = "OutlineColor_TempBlur";
                    tempDescAll.clearBuffer = false;
                    TextureHandle tempRtAll = renderGraph.CreateTexture(tempDescAll);

                    using (var builder =
                           renderGraph.AddRasterRenderPass<BlurPassData>("Outline Color Horizontal", out var passData))
                    {
                        passData.srcTexture = rt1;
                        passData.material = expandMaterialAllChannels;
                        passData.passIndex = 0;
                        builder.UseTexture(rt1);
                        builder.SetRenderAttachment(tempRtAll, 0);
                        builder.SetRenderFunc((BlurPassData data, RasterGraphContext context) =>
                            Blitter.BlitTexture(context.cmd, data.srcTexture, new Vector4(1, 1, 0, 0), data.material,
                                data.passIndex));
                    }

                    using (var builder =
                           renderGraph.AddRasterRenderPass<BlurPassData>("Outline Color Vertical", out var passData))
                    {
                        passData.srcTexture = tempRtAll;
                        passData.material = expandMaterialAllChannels;
                        passData.passIndex = 1;
                        builder.UseTexture(tempRtAll);
                        builder.SetRenderAttachment(rt1, 0);
                        builder.SetGlobalTextureAfterPass(rt1, Shader.PropertyToID(OUTLINE_COLOR_NAME));
                        builder.SetRenderFunc((BlurPassData data, RasterGraphContext context) =>
                            Blitter.BlitTexture(context.cmd, data.srcTexture, new Vector4(1, 1, 0, 0), data.material,
                                data.passIndex));
                    }
                }

                // --- 4. Expand Depth Pass (Depth / depthRt) ---
                if (doExpandDepth)
                {
                    TextureDesc tempDepthDesc = new TextureDesc(depthDesc.width, depthDesc.height);
                    tempDepthDesc.colorFormat = GraphicsFormat.None; // Must be None for pure Depth
                    tempDepthDesc.depthBufferBits = DepthBits.Depth32;
                    tempDepthDesc.msaaSamples = MSAASamples.None;
                    tempDepthDesc.name = "OutlineDepth_TempExpand";
                    tempDepthDesc.clearBuffer = false;
                    TextureHandle tempDepthRt = renderGraph.CreateTexture(tempDepthDesc);

                    using (var builder =
                           renderGraph.AddRasterRenderPass<BlurPassData>("Outline Depth Horizontal", out var passData))
                    {
                        passData.srcTexture = depthRt;
                        passData.material = expandMaterialDepth;
                        passData.passIndex = 0;

                        builder.UseTexture(depthRt);
                        // Notice: NO Color Attachment! Only Depth Attachment!
                        builder.SetRenderAttachmentDepth(tempDepthRt);

                        builder.SetRenderFunc((BlurPassData data, RasterGraphContext context) =>
                        {
                            Blitter.BlitTexture(context.cmd, data.srcTexture, new Vector4(1, 1, 0, 0),
                                data.material, data.passIndex);
                        });
                    }

                    using (var builder =
                           renderGraph.AddRasterRenderPass<BlurPassData>("Outline Depth Vertical", out var passData))
                    {
                        passData.srcTexture = tempDepthRt;
                        passData.material = expandMaterialDepth;
                        passData.passIndex = 1;

                        builder.UseTexture(tempDepthRt);
                        builder.SetRenderAttachmentDepth(depthRt);

                        // Export the depth globally now that it's expanded
                        builder.SetGlobalTextureAfterPass(depthRt, Shader.PropertyToID(OUTLINE_DEPTH_NAME));

                        builder.SetRenderFunc((BlurPassData data, RasterGraphContext context) =>
                        {
                            Blitter.BlitTexture(context.cmd, data.srcTexture, new Vector4(1, 1, 0, 0),
                                data.material, data.passIndex);
                        });
                    }
                }
            }
        }

        public Material overrideMaterial;
        public Material expandMaterialAllChannels;
        public Material expandMaterialDepth;
        public LayerMask outlineLayer;
        private OutlineDataPass pass;

        public override void Create()
        {
            Debug.Log($"LayerMask for outlines: {outlineLayer}");
            pass = new OutlineDataPass(overrideMaterial, expandMaterialAllChannels, expandMaterialDepth, outlineLayer)
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pass?.Dispose();
            }
        }
    }
}
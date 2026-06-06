using Map.GeometryGeneration;
using UnityEngine;

namespace Map.OutlineEffect
{
    public abstract class AOutlineableObjectBase : AObjectWithGeometry
    {
        private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int InnerColor = Shader.PropertyToID("_InnerColor");
        private static readonly int TextureId = Shader.PropertyToID("_TextureId");
        private static readonly int PlayerColor = Shader.PropertyToID("_PlayerColor");

        protected abstract string OutlineLayerName();
        protected abstract string OutlineTransparentLayerName();

        private int defaultLayer = -1;
        private int outlineLayer;
        private int outlineTransparentLayer;

        private Renderer objRenderer;

        private Color outlineColor;
        private Color innerColor;
        private int textureId;
        private Color playerColor;

        protected new void Init()
        {
            base.Init();
            if (defaultLayer == -1)
            {
                defaultLayer = gameObject.layer;
                outlineLayer = LayerMask.NameToLayer(OutlineLayerName());
                outlineTransparentLayer = LayerMask.NameToLayer(OutlineTransparentLayerName());
            }

            objRenderer = GetComponent<Renderer>();

            // Apply initial material properties
            SetMaterialPropertyBlock();
        }

        public void SetPlayerColor(Color color)
        {
            playerColor = color;
            SetMaterialPropertyBlock();
        }

        public void SetOutlineParameters(Color colorOutline, Color colorInner, int outlineTextureId)
        {
            outlineColor = colorOutline;
            innerColor = colorInner;
            textureId = outlineTextureId;
            SetMaterialPropertyBlock();
        }

        public void SetOutlineParameters(Constants.OutlineData outlineData)
        {
            outlineColor = outlineData.OutlineColor;
            innerColor = outlineData.InnerColor;
            textureId = outlineData.TextureId;
            SetMaterialPropertyBlock();
        }

        public void SetBaseLayer() => gameObject.layer = defaultLayer;

        public void SetOutlineLayer() => gameObject.layer = outlineLayer;

        public void SetOutlineTransparentLayer() => gameObject.layer = outlineTransparentLayer;

        private void SetMaterialPropertyBlock()
        {
            if (objRenderer == null)
            {
                Debug.LogError("Renderer is null");
                return;
            }

            var mpb = new MaterialPropertyBlock();
            objRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(PlayerColor, playerColor);
            mpb.SetColor(OutlineColor, outlineColor);
            mpb.SetColor(InnerColor, innerColor);
            mpb.SetFloat(TextureId, textureId);
            objRenderer.SetPropertyBlock(mpb);
        }
    }
}
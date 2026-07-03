using UnityEngine;

namespace Map.OutlineEffect
{
    public abstract class AOutlineableObjectBase : MonoBehaviour
    {
        private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int InnerColor = Shader.PropertyToID("_InnerColor");
        private static readonly int TextureId = Shader.PropertyToID("_TextureId");
        private static readonly int PlayerColor = Shader.PropertyToID("_PlayerColor");
        private static readonly int CustomColorId = Shader.PropertyToID("_CustomColor");

        public bool CurrentlyHoverable
        {
            get => currentlyHoverable;
            set
            {
                currentlyHoverable = value;
                switch (state)
                {
                    case State.None:
                        SetBaseLayer();
                        break;
                    case State.Outline:
                        SetOutlineLayer();
                        break;
                    case State.OutlineTransparent:
                        SetOutlineTransparentLayer();
                        break;
                }
            }
        }

        private enum State
        {
            None,
            Outline,
            OutlineTransparent,
        }

        private State state;

        private bool currentlyHoverable = true;

        private int defaultLayer = -1;
        private int outlineLayer;
        private int outlineTransparentLayer;

        private int hoverBaseLayer;
        private int hoverOutlineLayer;
        private int hoverOutlineTransparentLayer;

        private Renderer objRenderer;
        private MaterialPropertyBlock mpb;

        private Color outlineColor;
        private Color innerColor;
        private int textureId;
        private Color playerColor;
        private Color customColor;

        protected void Init()
        {
            defaultLayer = LayerMask.NameToLayer("Default");
            outlineLayer = LayerMask.NameToLayer("Outline");
            outlineTransparentLayer = LayerMask.NameToLayer("Outline Transparent");

            hoverBaseLayer = LayerMask.NameToLayer("Hoverable");
            hoverOutlineLayer = LayerMask.NameToLayer("Hoverable Outline");
            hoverOutlineTransparentLayer = LayerMask.NameToLayer("Hoverable Outline Transparent");

            objRenderer = GetComponent<Renderer>();

            // Apply initial material properties
            SetMaterialPropertyBlock();
            SetBaseLayer();
            state = State.None;
        }

        public void SetPlayerColor(Color color)
        {
            playerColor = color;
            SetMaterialPropertyBlock();
        }

        public void SetCustomColor(Color color)
        {
            customColor = color;
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

        public void SetBaseLayer()
        {
            gameObject.layer = currentlyHoverable ? hoverBaseLayer : defaultLayer;
            state = State.None;
        }

        public void SetOutlineLayer()
        {
            gameObject.layer = currentlyHoverable ? hoverOutlineLayer : outlineLayer;
            state = State.Outline;
        }

        public void SetOutlineTransparentLayer()
        {
            gameObject.layer = currentlyHoverable ? hoverOutlineTransparentLayer : outlineTransparentLayer;
            state = State.OutlineTransparent;
        }

        public void SetMaterial(Material material)
        {
            if (objRenderer != null)
            {
                objRenderer.sharedMaterial = material;
            }
        }

        private void SetMaterialPropertyBlock()
        {
            if (objRenderer == null)
            {
                return;
            }

            mpb ??= new MaterialPropertyBlock();
            objRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(PlayerColor, playerColor);
            mpb.SetColor(OutlineColor, outlineColor);
            mpb.SetColor(InnerColor, innerColor);
            mpb.SetColor(CustomColorId, customColor);
            mpb.SetFloat(TextureId, textureId);
            objRenderer.SetPropertyBlock(mpb);
        }
    }
}
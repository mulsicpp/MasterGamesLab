using UnityEngine;

namespace Map.OutlineEffect
{
    [ExecuteAlways]
    public class OutlineObjectData : MonoBehaviour
    {
        private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int InnerColor = Shader.PropertyToID("_InnerColor");
        private static readonly int TextureId = Shader.PropertyToID("_TextureId");

        public Color outlineColor = Color.white;
        public Color innerColor = new Color(1, 0, 0, 0.5f);
        public int textureId = 0;

        private Renderer objRenderer;

        private void Start()
        {
            objRenderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (objRenderer != null)
            {
                var mpb = new MaterialPropertyBlock();
                objRenderer.GetPropertyBlock(mpb);
                mpb.SetColor(OutlineColor, outlineColor);
                mpb.SetColor(InnerColor, innerColor);
                mpb.SetFloat(TextureId, textureId);
                objRenderer.SetPropertyBlock(mpb);
            }
        }
    }
}
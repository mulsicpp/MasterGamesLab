using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class AlphaPickableElement : VisualElement
{
    public override bool ContainsPoint(Vector2 localPoint)
    {
        if (!base.ContainsPoint(localPoint)) return false;

        Background bg = style.backgroundImage.value;
        Texture2D tex = bg.texture;
        if (tex == null) return true;

        float xPct = localPoint.x / layout.width;
        float yPct = 1.0f - (localPoint.y / layout.height);

        int pixelX = Mathf.Clamp(Mathf.RoundToInt(xPct * tex.width), 0, tex.width - 1);
        int pixelY = Mathf.Clamp(Mathf.RoundToInt(yPct * tex.height), 0, tex.height - 1);

        Color pixelColor = tex.GetPixel(pixelX, pixelY);
        return pixelColor.a > 0.1f;
    }
}
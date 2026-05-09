using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement] // Automatically generates the factory
public partial class CooldownIcon : VisualElement
{
    // Attributes can be placed directly on properties or fields
    [UxmlAttribute("cooldown")]
    private float cooldownPercent = 1f;

    public float CooldownPercent
    {
        get => cooldownPercent;
        set
        {
            cooldownPercent = Mathf.Clamp01(value);
            MarkDirtyRepaint();
        }
    }

    private Color overlayColor = new Color(0, 0, 0, 0.6f);

    public CooldownIcon()
    {
        style.unityBackgroundImageTintColor = Color.white;
        generateVisualContent += OnGenerateVisualContent;
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        if (CooldownPercent <= 0f)
            return;

        var painter = ctx.painter2D;

        float size = Mathf.Min(resolvedStyle.width, resolvedStyle.height);
        Vector2 center = new Vector2(resolvedStyle.width / 2, resolvedStyle.height / 2);
        float radius = size * 0.5f;

        int segments = 64;
        // Logic remains identical to your original mesh generation
        float angleStep = 360f * CooldownPercent / segments;

        painter.fillColor = overlayColor;
        painter.BeginPath();

        painter.MoveTo(center);

        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Deg2Rad * (90f - i * angleStep); // start at top
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            painter.LineTo(point);
        }

        painter.ClosePath();
        painter.Fill();
    }
}
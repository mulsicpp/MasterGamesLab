using UnityEngine;
using UnityEngine.UIElements;

public class CooldownIcon : VisualElement
{
    public new class UxmlFactory : UxmlFactory<CooldownIcon, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlFloatAttributeDescription cooldownAttr =
            new UxmlFloatAttributeDescription { name = "cooldown", defaultValue = 1f };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            ((CooldownIcon)ve).CooldownPercent =
                Mathf.Clamp01(cooldownAttr.GetValueFromBag(bag, cc));
        }
    }

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
        //style.backgroundScaleMode = ScaleMode.ScaleToFit;
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

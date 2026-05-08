using UnityEngine;
using UnityEngine.UIElements;

public class PartialCircle : VisualElement
{
    public new class UxmlFactory : UxmlFactory<PartialCircle, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlFloatAttributeDescription m_Fill = new UxmlFloatAttributeDescription { name = "fill", defaultValue = 1f };
        UxmlColorAttributeDescription m_Color = new UxmlColorAttributeDescription { name = "color", defaultValue = Color.white };

        UxmlFloatAttributeDescription m_Arc2Fill = new UxmlFloatAttributeDescription { name = "arc2-fill", defaultValue = 0f };
        UxmlColorAttributeDescription m_Arc2Color = new UxmlColorAttributeDescription { name = "arc2-color", defaultValue = Color.clear };

        UxmlFloatAttributeDescription m_Arc3Fill = new UxmlFloatAttributeDescription { name = "arc3-fill", defaultValue = 0f };
        UxmlColorAttributeDescription m_Arc3Color = new UxmlColorAttributeDescription { name = "arc3-color", defaultValue = Color.clear };

        UxmlFloatAttributeDescription m_Thickness = new UxmlFloatAttributeDescription { name = "thickness", defaultValue = 0.1f };
        UxmlColorAttributeDescription m_OutlineColor = new UxmlColorAttributeDescription { name = "outline-color", defaultValue = Color.clear };
        UxmlFloatAttributeDescription m_OutlineThickness = new UxmlFloatAttributeDescription { name = "outline-thickness", defaultValue = 0f };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var el = ve as PartialCircle;

            el.Fill = Mathf.Clamp01(m_Fill.GetValueFromBag(bag, cc));
            el.Color = m_Color.GetValueFromBag(bag, cc);

            el.Arc2Fill = Mathf.Clamp01(m_Arc2Fill.GetValueFromBag(bag, cc));
            el.Arc2Color = m_Arc2Color.GetValueFromBag(bag, cc);

            el.Arc3Fill = Mathf.Clamp01(m_Arc3Fill.GetValueFromBag(bag, cc));
            el.Arc3Color = m_Arc3Color.GetValueFromBag(bag, cc);

            el.Thickness = Mathf.Clamp01(m_Thickness.GetValueFromBag(bag, cc));
            el.OutlineColor = m_OutlineColor.GetValueFromBag(bag, cc);
            el.OutlineThickness = Mathf.Clamp01(m_OutlineThickness.GetValueFromBag(bag, cc));
        }
    }

    private float _fill = 1f;
    private Color _color = Color.white;
    private float _thickness = 0.1f;

    private float _arc2Fill = 0f;
    private Color _arc2Color = Color.clear;

    private float _arc3Fill = 0f;
    private Color _arc3Color = Color.clear;

    private Color _outlineColor = Color.clear;
    private float _outlineThickness = 0f;

    public float Fill { get => _fill; set { _fill = Mathf.Clamp01(value); MarkDirtyRepaint(); } }
    public Color Color { get => _color; set { _color = value; MarkDirtyRepaint(); } }

    public float Arc2Fill { get => _arc2Fill; set { _arc2Fill = Mathf.Clamp01(value); MarkDirtyRepaint(); } }
    public Color Arc2Color { get => _arc2Color; set { _arc2Color = value; MarkDirtyRepaint(); } }

    public float Arc3Fill { get => _arc3Fill; set { _arc3Fill = Mathf.Clamp01(value); MarkDirtyRepaint(); } }
    public Color Arc3Color { get => _arc3Color; set { _arc3Color = value; MarkDirtyRepaint(); } }

    public float Thickness { get => _thickness; set { _thickness = Mathf.Clamp01(value); MarkDirtyRepaint(); } }
    public Color OutlineColor { get => _outlineColor; set { _outlineColor = value; MarkDirtyRepaint(); } }
    public float OutlineThickness { get => _outlineThickness; set { _outlineThickness = Mathf.Clamp01(value); MarkDirtyRepaint(); } }

    public PartialCircle()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        var painter = ctx.painter2D;

        float size = Mathf.Min(resolvedStyle.width, resolvedStyle.height);
        Vector2 center = new Vector2(resolvedStyle.width / 2f, resolvedStyle.height / 2f);

        float outerRadius = size / 2f;
        float arcOuter = outerRadius;
        float arcInner = arcOuter - Thickness * size;

        float outlineSize = OutlineThickness * size;
        float outerOutlineOuter = arcOuter;
        float outerOutlineInner = arcOuter - outlineSize;

        float innerOutlineOuter = arcInner + outlineSize;
        float innerOutlineInner = arcInner;

        if (arcInner <= 0f) return;

        int segments1 = Mathf.Max(3, Mathf.CeilToInt(Fill * 100f));
        float sweep1 = 360f * Fill;
        float startAngle = 135f;
        int segments2 = Mathf.Max(3, Mathf.CeilToInt(Arc2Fill * 100f));
        float sweep2 = 360f * Arc2Fill;
        int segments3 = Mathf.Max(3, Mathf.CeilToInt(Arc3Fill * 100f));
        float sweep3 = 360f * Arc3Fill;

        // Draw filled arc ring
        painter.BeginPath();
        for (int i = segments1; i >= 0; i--)
        {
            float t = (float)i / segments1;
            float angle = Mathf.Deg2Rad * (startAngle - t * sweep1);
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * arcOuter;
            if (i == segments1) painter.MoveTo(point);
            else painter.LineTo(point);
        }

        for (int i = 0; i <= segments1; i++)
        {
            float t = (float)i / segments1;
            float angle = Mathf.Deg2Rad * (startAngle - t * sweep1);
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * arcInner;
            painter.LineTo(point);
        }

        painter.ClosePath();
        painter.fillColor = Color;
        painter.Fill();

        // Draw filled arc ring2
        painter.BeginPath();
        for (int i = segments2; i >= 0; i--)
        {
            float t = (float)i / segments2;
            float angle = Mathf.Deg2Rad * (startAngle - t * sweep2);
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * arcOuter;
            if (i == segments2) painter.MoveTo(point);
            else painter.LineTo(point);
        }

        for (int i = 0; i <= segments2; i++)
        {
            float t = (float)i / segments2;
            float angle = Mathf.Deg2Rad * (startAngle - t * sweep2);
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * arcInner;
            painter.LineTo(point);
        }

        painter.ClosePath();
        painter.fillColor = Arc2Color;
        painter.Fill();



// Draw filled arc ring3
        painter.BeginPath();
        for (int i = segments3; i >= 0; i--)
        {
            float t = (float)i / segments3;
            float angle = Mathf.Deg2Rad * (startAngle - t * sweep3);
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * arcOuter;
            if (i == segments3) painter.MoveTo(point);
            else painter.LineTo(point);
        }

        for (int i = 0; i <= segments3; i++)
        {
            float t = (float)i / segments3;
            float angle = Mathf.Deg2Rad * (startAngle - t * sweep3);
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * arcInner;
            painter.LineTo(point);
        }

        painter.ClosePath();
        painter.fillColor = Arc3Color;
        painter.Fill();





        // Draw outer outline ring
        if (OutlineColor.a > 0f && outlineSize > 0f)
        {
            painter.BeginPath();
            for (int i = segments1; i >= 0; i--)
            {
                float t = (float)i / segments1;
                float angle = Mathf.Deg2Rad * (startAngle - t * sweep1);
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * outerOutlineOuter;
                if (i == segments1) painter.MoveTo(point);
                else painter.LineTo(point);
            }
            for (int i = 0; i <= segments1; i++)
            {
                float t = (float)i / segments1;
                float angle = Mathf.Deg2Rad * (startAngle - t * sweep1);
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * outerOutlineInner;
                painter.LineTo(point);
            }
            painter.ClosePath();
            painter.fillColor = OutlineColor;
            painter.Fill();
        }

        // Draw inner outline ring
        if (OutlineColor.a > 0f && outlineSize > 0f)
        {
            painter.BeginPath();
            for (int i = segments1; i >= 0; i--)
            {
                float t = (float)i / segments1;
                float angle = Mathf.Deg2Rad * (startAngle - t * sweep1);
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * innerOutlineOuter;
                if (i == segments1) painter.MoveTo(point);
                else painter.LineTo(point);
            }
            for (int i = 0; i <= segments1; i++)
            {
                float t = (float)i / segments1;
                float angle = Mathf.Deg2Rad * (startAngle - t * sweep1);
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * innerOutlineInner;
                painter.LineTo(point);
            }
            painter.ClosePath();
            painter.fillColor = OutlineColor;
            painter.Fill();
        }

        // Draw side lines at arc start and end
        Vector2 outerStart = center + new Vector2(Mathf.Cos(Mathf.Deg2Rad * (startAngle)), Mathf.Sin(Mathf.Deg2Rad * (startAngle))) * arcOuter;
        Vector2 innerStart = center + new Vector2(Mathf.Cos(Mathf.Deg2Rad * (startAngle)), Mathf.Sin(Mathf.Deg2Rad * (startAngle))) * arcInner;
        Vector2 outerEnd = center + new Vector2(Mathf.Cos(Mathf.Deg2Rad * (startAngle - sweep1)), Mathf.Sin(Mathf.Deg2Rad * (startAngle - sweep1))) * arcOuter;
        Vector2 innerEnd = center + new Vector2(Mathf.Cos(Mathf.Deg2Rad * (startAngle - sweep1)), Mathf.Sin(Mathf.Deg2Rad * (startAngle - sweep1))) * arcInner;

        painter.strokeColor = OutlineColor;
        painter.lineWidth = outlineSize;
        painter.BeginPath();
        painter.MoveTo(innerStart);
        painter.LineTo(outerStart);
        painter.MoveTo(innerEnd);
        painter.LineTo(outerEnd);
        painter.Stroke();
    }
}

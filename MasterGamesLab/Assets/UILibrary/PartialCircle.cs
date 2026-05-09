using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class PartialCircle : VisualElement
{
    // Using attributes directly on the properties replaces the entire UxmlTraits class
    [UxmlAttribute("fill")]
    public float Fill { get => _fill; set { _fill = Mathf.Clamp01(value); MarkDirtyRepaint(); } }
    
    [UxmlAttribute("color")]
    public Color Color { get => _color; set { _color = value; MarkDirtyRepaint(); } }

    [UxmlAttribute("arc2-fill")]
    public float Arc2Fill { get => _arc2Fill; set { _arc2Fill = Mathf.Clamp01(value); MarkDirtyRepaint(); } }
    
    [UxmlAttribute("arc2-color")]
    public Color Arc2Color { get => _arc2Color; set { _arc2Color = value; MarkDirtyRepaint(); } }

    [UxmlAttribute("arc3-fill")]
    public float Arc3Fill { get => _arc3Fill; set { _arc3Fill = Mathf.Clamp01(value); MarkDirtyRepaint(); } }
    
    [UxmlAttribute("arc3-color")]
    public Color Arc3Color { get => _arc3Color; set { _arc3Color = value; MarkDirtyRepaint(); } }

    [UxmlAttribute("thickness")]
    public float Thickness { get => _thickness; set { _thickness = Mathf.Clamp01(value); MarkDirtyRepaint(); } }
    
    [UxmlAttribute("outline-color")]
    public Color OutlineColor { get => _outlineColor; set { _outlineColor = value; MarkDirtyRepaint(); } }
    
    [UxmlAttribute("outline-thickness")]
    public float OutlineThickness { get => _outlineThickness; set { _outlineThickness = Mathf.Clamp01(value); MarkDirtyRepaint(); } }

    // Private backing fields
    private float _fill = 1f;
    private Color _color = Color.white;
    private float _thickness = 0.1f;
    private float _arc2Fill = 0f;
    private Color _arc2Color = Color.clear;
    private float _arc3Fill = 0f;
    private Color _arc3Color = Color.clear;
    private Color _outlineColor = Color.clear;
    private float _outlineThickness = 0f;

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

        // --- Drawing Logic (remains unchanged) ---
        
        // Draw filled arc ring
        DrawArcRing(painter, center, arcOuter, arcInner, startAngle, sweep1, segments1, Color);

        // Draw filled arc ring2
        DrawArcRing(painter, center, arcOuter, arcInner, startAngle, sweep2, segments2, Arc2Color);

        // Draw filled arc ring3
        DrawArcRing(painter, center, arcOuter, arcInner, startAngle, sweep3, segments3, Arc3Color);

        // Draw outer outline ring
        if (OutlineColor.a > 0f && outlineSize > 0f)
        {
            DrawArcRing(painter, center, outerOutlineOuter, outerOutlineInner, startAngle, sweep1, segments1, OutlineColor);
            DrawArcRing(painter, center, innerOutlineOuter, innerOutlineInner, startAngle, sweep1, segments1, OutlineColor);
        }

        // Draw side lines
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

    // Optional: Helper to keep the generate content method cleaner
    private void DrawArcRing(Painter2D painter, Vector2 center, float outer, float inner, float startAngle, float sweep, int segments, Color color)
    {
        if (color.a <= 0) return;
        
        painter.BeginPath();
        for (int i = segments; i >= 0; i--)
        {
            float t = (float)i / segments;
            float angle = Mathf.Deg2Rad * (startAngle - t * sweep);
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * outer;
            if (i == segments) painter.MoveTo(point);
            else painter.LineTo(point);
        }
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Deg2Rad * (startAngle - t * sweep);
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * inner;
            painter.LineTo(point);
        }
        painter.ClosePath();
        painter.fillColor = color;
        painter.Fill();
    }
}
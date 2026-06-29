using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class Compass : Button
{
    // === Arrow 1 Properties ===
    private float arrowAngle = 0f;
    [UxmlAttribute("arrow-angle")]
    public float ArrowAngle
    {
        get => arrowAngle;
        set { arrowAngle = value; MarkDirtyRepaint(); }
    }

    private Color arrowColor = Color.red;
    [UxmlAttribute("arrow-color")]
    public Color ArrowColor
    {
        get => arrowColor;
        set { arrowColor = value; MarkDirtyRepaint(); }
    }

    private float arrowThickness = 2f;
    [UxmlAttribute("arrow-thickness")]
    public float ArrowThickness
    {
        get => arrowThickness;
        set { arrowThickness = value; MarkDirtyRepaint(); }
    }

    private float arrowLength = 0.75f;
    [UxmlAttribute("arrow-length")]
    public float ArrowLength
    {
        get => arrowLength;
        set { arrowLength = Mathf.Clamp01(value); MarkDirtyRepaint(); }
    }


    public Compass()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        var painter = ctx.painter2D;

        float size = Mathf.Min(resolvedStyle.width, resolvedStyle.height);
        Vector2 center = new Vector2(resolvedStyle.width / 2, resolvedStyle.height / 2);

        DrawArrow1(painter, center, size, arrowAngle, arrowColor, arrowThickness, arrowLength);
    }

    private void DrawArrow1(Painter2D painter, Vector2 center, float size, float angleDegrees, Color color, float thickness, float lengthPercent)
    {
        float arrowRadians = Mathf.Deg2Rad * angleDegrees;
        float outerRadius = size * 0.5f;
        float arrowLengthAbs = lengthPercent * outerRadius;

        Vector2 dir = new Vector2(Mathf.Cos(arrowRadians), Mathf.Sin(arrowRadians));
        Vector2 perp = new Vector2(-dir.y, dir.x);

        Vector2 tip = center + dir * outerRadius;
        Vector2 tail = center + dir * (outerRadius - arrowLengthAbs);

        float baseThickness = thickness * 0.5f;
        float tipThickness = thickness;

        Vector2 tailLeft = tail - perp * baseThickness;
        Vector2 tailRight = tail + perp * baseThickness;
        Vector2 tipLeft = tip - perp * tipThickness;
        Vector2 tipRight = tip + perp * tipThickness;

        painter.fillColor = color;

        painter.BeginPath();
        painter.MoveTo(tailLeft);
        painter.LineTo(tipLeft);
        painter.LineTo(tipRight);
        painter.LineTo(tailRight);
        painter.ClosePath();
        painter.Fill();
    }
}
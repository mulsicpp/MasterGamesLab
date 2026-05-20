using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class LoadingButton : Button
{
    private bool m_IsLoading = false;
    private IVisualElementScheduledItem m_AnimationTask;
    private float m_AnimationTime = 0f;

    private Texture2D icon;

    [UxmlAttribute("spinner-color")]
    public Color spinnerColor { get; set; } = new Color(0f, 0.34f, 0.91f, 1f); // Google Blue

    [UxmlAttribute("stroke-width-percent")]
    public float strokeWidthPercent { get; set; } = 0.08f;

    [UxmlAttribute("spinner-radius-percent")]
    public float spinnerRadiusPercent { get; set; } = 0.7f;

    // Controls overall speed of the animation cycle

    [UxmlAttribute("snap-intensity")]
    public float snapIntensity { get; set; } = 3f;

    [UxmlAttribute("linear-spin")]
    public float linearSpin { get; set; } = 5f;

    [UxmlAttribute("max-distance-between-head-and-tail")]
    public float maxAngle { get; set; } = 324f;

    [UxmlAttribute("angle-offset")]
    public float angleOffset { get; set; } = 18f;

    private float headAngle;
    private float tailAngle = 0f;

    public LoadingButton()
    {
        style.marginTop = 0; style.marginBottom = 0; style.marginLeft = 0; style.marginRight = 0;
        style.paddingTop = 0; style.paddingBottom = 0; style.paddingLeft = 0; style.paddingRight = 0;
        style.justifyContent = Justify.Center;
        style.alignItems = Align.Center;

        generateVisualContent += OnGenerateVisualContent;
        //clicked += OnButtonClicked;

        m_AnimationTask = schedule.Execute(AnimateSpinner).Every(16);
        m_AnimationTask.Pause();
    }

    private void OnButtonClicked()
    {
        SetLoading(!m_IsLoading);
    }

    public void SetLoading(bool loading)
    {
        if (m_IsLoading == loading) return;
        m_IsLoading = loading;

        if (m_IsLoading)
        {
            if (resolvedStyle.backgroundImage != null && resolvedStyle.backgroundImage.texture != null && icon == null)
            {
                icon = resolvedStyle.backgroundImage.texture;
            }
            tailAngle = 0;
            headAngle = angleOffset;
            text = string.Empty;
            style.backgroundImage = null;
            m_AnimationTime = 0f;
            m_AnimationTask.Resume();
        }
        else
        {
            if (icon != null)
            {
                style.backgroundImage = new StyleBackground(icon);
            }
            m_AnimationTask.Pause();
            MarkDirtyRepaint();
        }
    }

    private void AnimateSpinner()
    {
        if (!m_IsLoading) return;

        // Progresses time continuously between 0.0 and 1.0 down a loop
        m_AnimationTime += Time.deltaTime;
        if (m_AnimationTime >= 1f)
        {
            m_AnimationTime -= 1f;
        }

        float p = Mathf.Max(1f, snapIntensity);
        if (m_AnimationTime < 0.5f)
        {
            float t = m_AnimationTime / 0.5f;
            float stepSize = p * Mathf.Pow(t, p - 1f) * maxAngle * (1f / 0.5f) * Time.deltaTime;
            headAngle += stepSize;

        }
        else
        {
            float t = (m_AnimationTime - 0.5f) / 0.5f;
            float stepSize = p * Mathf.Pow(t, p - 1f) * maxAngle * (1f / 0.5f) * Time.deltaTime;
            tailAngle += stepSize;

        }
        headAngle = (headAngle + linearSpin) % 360f;
        tailAngle = (tailAngle + linearSpin) % 360f;

        MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        if (!m_IsLoading) return;

        var painter = mgc.painter2D;

        // Dynamic Sizing based on button dimensions
        float centerX = contentRect.width / 2f;
        float centerY = contentRect.height / 2f;
        float maxHalfSize = Mathf.Min(centerX, centerY);

        float clampedRadiusPercent = Mathf.Clamp01(spinnerRadiusPercent);
        float clampedStrokePercent = Mathf.Clamp01(strokeWidthPercent);

        float finalRadius = maxHalfSize * clampedRadiusPercent;
        float finalStrokeWidth = finalRadius * clampedStrokePercent;

        if (finalRadius + (finalStrokeWidth / 2f) > maxHalfSize)
        {
            finalRadius = maxHalfSize - (finalStrokeWidth / 2f);
        }

        painter.lineWidth = finalStrokeWidth;
        painter.strokeColor = spinnerColor;
        painter.lineCap = LineCap.Round;

        painter.BeginPath();

        // Add a linear secondary continuous rotation factor so the entire loop moves around the circle


        float startAngle = tailAngle;
        float endAngle = headAngle;


        // Prevent zero-width tiny flickering anomalies when lines overlap perfectly
        if (Mathf.Abs(endAngle - startAngle) < 10f)
        {
            endAngle = startAngle + 10f;
        }

        painter.Arc(new Vector2(centerX, centerY), finalRadius, startAngle, endAngle);
        painter.Stroke();
    }
}
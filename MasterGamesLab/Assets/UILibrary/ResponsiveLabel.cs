using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement] // This replaces the UxmlFactory class
public partial class ResponsiveLabel : Label
{
    // Adding [UxmlAttribute] replaces the UxmlTraits class
    [UxmlAttribute("height-percentage")]
    private float _heightPercentage = 0.5f;

    public float HeightPercentage
    {
        get => _heightPercentage;
        set
        {
            _heightPercentage = Mathf.Clamp01(value);
            UpdateFontSize();
        }
    }

    public ResponsiveLabel()
    {
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        UpdateFontSize();
    }

    private void UpdateFontSize()
    {
        var parent = this.parent;
        if (parent != null)
        {
            float parentHeight = parent.resolvedStyle.height;
            if (parentHeight > 0)
            {
                // Note: resolvedStyle is more reliable than layout.height here
                this.style.fontSize = parentHeight * _heightPercentage;
            }
        }
    }
}
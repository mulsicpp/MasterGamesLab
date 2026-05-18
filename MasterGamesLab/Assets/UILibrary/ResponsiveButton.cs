using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement] // This replaces the UxmlFactory class
public partial class ResponsiveButton : Button
{
    // Adding [UxmlAttribute] replaces the UxmlTraits class
    [UxmlAttribute("height-percentage")]
    private float _heightPercentage = 0.5f;

    public float HeightPercentage
    {
        get => _heightPercentage;
        set
        {
            // Clamp between 0.0 (0%) and 1.0 (100%) of parent height
            _heightPercentage = Mathf.Clamp01(value);
            UpdateFontSize();
        }
    }

    public ResponsiveButton()
    {
        // Register layout change callback to recalculate when screen scales/resizes
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
                // Set the font size based on the resolved layout height of the parent container
                this.style.fontSize = parentHeight * _heightPercentage;
            }
        }
    }
}
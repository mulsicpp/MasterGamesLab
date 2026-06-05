using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement] // Replaces the UxmlFactory class
public partial class SquareWidthElement : VisualElement
{
    public SquareWidthElement()
    {
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        float width = resolvedStyle.width;

        if (width > 0)
        {
            style.height = width;    
        }
    }
}
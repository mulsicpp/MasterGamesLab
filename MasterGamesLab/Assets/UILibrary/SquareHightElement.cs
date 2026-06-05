using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement] // Replaces the UxmlFactory class
public partial class SquareHightElement : VisualElement
{
    public SquareHightElement()
    {
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        float height = resolvedStyle.height;

        if (height > 0)
        {
            style.width = height;
        }
    }
}
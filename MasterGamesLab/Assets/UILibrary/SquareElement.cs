using UnityEngine;
using UnityEngine.UIElements;

public class SquareElement : VisualElement
{
    public new class UxmlFactory : UxmlFactory<SquareElement, UxmlTraits> { }

    public SquareElement()
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

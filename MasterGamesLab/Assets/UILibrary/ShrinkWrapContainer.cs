using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class ShrinkWrapContainer : VisualElement
{
    private bool _isCalculating = false;

    public ShrinkWrapContainer()
    {
        // Listen for when any child changes size or wraps rows
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        // Prevent infinite layout recursion loops
        if (_isCalculating) return;
        if (childCount == 0) return;

        _isCalculating = true;

        float maxBottomEdge = 0f;

        // Loop through all children to find out how far down they extend
        for (int i = 0; i < childCount; i++)
        {
            VisualElement child = this[i];
            
            // Skip elements that are hidden or not participating in layout
            if (child.style.display == DisplayStyle.None) continue;

            // child.layout holds the position relative to this container
            float childBottom = child.layout.yMax;
            
            if (childBottom > maxBottomEdge)
            {
                maxBottomEdge = childBottom;
            }
        }

        // If we found a valid height, snap the container's height to match it perfectly
        if (maxBottomEdge > 0)
        {
            style.height = maxBottomEdge;
        }

        _isCalculating = false;
    }
}
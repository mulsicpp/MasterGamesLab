using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class ShrinkWrapContainer : GroupBox
{
    private bool _isCalculating = false;

    public ShrinkWrapContainer()
    {
        // Listen to layout updates on this container directly
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        RecalculateHeight();
    }

    // Call this manually from your Hide script if you want an instant snap!
    public void RecalculateHeight()
    {
        if (_isCalculating) return;
        _isCalculating = true;

        float maxBottomEdge = 0f;
        int visibleChildren = 0;

        for (int i = 0; i < childCount; i++)
        {
            VisualElement child = this[i];
            
            // CRITICAL FIX: Check resolvedStyle instead of raw style properties
            if (child.resolvedStyle.display == DisplayStyle.None) 
                continue;

            float childBottom = child.layout.yMax;
            
            if (childBottom > maxBottomEdge)
            {
                maxBottomEdge = childBottom;
                visibleChildren++;
            }
        }

        // If no children are left visible, collapse the container height to 0
        if (visibleChildren == 0)
        {
            style.height = 0f;
        }
        else if (maxBottomEdge > 0)
        {
            style.height = maxBottomEdge;
        }

        _isCalculating = false;
    }
}
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class SquareElement : VisualElement
{
    public SquareElement()
    {
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        // Use the new layout dimensions from the event payload
        float newWidth = evt.newRect.width;
        float newHeight = evt.newRect.height;

        // 1. Guard check: Avoid running logic on uninitialized layout frames
        if (newWidth <= 0 && newHeight <= 0) return;

        // 2. Check what style constraints are driving the UI Layout engine
        bool widthIsAuto = style.width.keyword == StyleKeyword.Auto;
        bool heightIsAuto = style.height.keyword == StyleKeyword.Auto;

        // Case A: Height is the hard driver (Width is Auto), or Height is simply larger
        if (!heightIsAuto && (widthIsAuto || newHeight > newWidth))
        {
            // Only update if it isn't already matching to kill the infinite loop risk
            if (Mathf.Abs(resolvedStyle.width - newHeight) > 0.1f)
            {
                style.width = newHeight;
            }
        }
        // Case B: Width is the hard driver (Height is Auto), or Width is simply larger
        else if (!widthIsAuto && (heightIsAuto || newWidth > newHeight))
        {
            if (Mathf.Abs(resolvedStyle.height - newWidth) > 0.1f)
            {
                style.height = newWidth;
            }
        }
    }
}
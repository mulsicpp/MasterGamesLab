using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class SquareElement : VisualElement
{
    public SquareElement()
    {
        // Force the layout engine to keep this element perfectly square (1:1 ratio)
        style.aspectRatio = 1f;
    }
}
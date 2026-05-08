using UnityEngine;
using UnityEngine.UIElements;

public class ResponsiveLabel : Label
{
    // Default 50% of parent height
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

    public new class UxmlFactory : UxmlFactory<ResponsiveLabel, UxmlTraits> { }

    public new class UxmlTraits : Label.UxmlTraits
    {
        UxmlFloatAttributeDescription m_HeightPercentage = new UxmlFloatAttributeDescription
        {
            name = "height-percentage",
            defaultValue = 0.5f
        };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var label = ve as ResponsiveLabel;
            label.HeightPercentage = m_HeightPercentage.GetValueFromBag(bag, cc);
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
                this.style.fontSize = parentHeight * HeightPercentage;
            }
        }
    }
}

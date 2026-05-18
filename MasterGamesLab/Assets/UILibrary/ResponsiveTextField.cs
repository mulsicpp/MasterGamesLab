using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class ResponsiveTextField : VisualElement
{
    private readonly Label _label;
    private readonly TextField _inputField;

    // --- CUSTOM EDITOR ATTRIBUTES ---
    
    [UxmlAttribute("label-text")]
    public string LabelText
    {
        get => _label.text;
        set => _label.text = value;
    }

    [UxmlAttribute("placeholder-text")]
    public string PlaceholderText
    {
        get => _inputField.textEdition.placeholder;
        set => _inputField.textEdition.placeholder = value; 
    }

    [UxmlAttribute("label-width-percentage")]
    private float _labelWidthPercentage = 0.5f; 
    public float LabelWidthPercentage
    {
        get => _labelWidthPercentage;
        set
        {
            _labelWidthPercentage = Mathf.Clamp(value, 0.05f, 1f);
            UpdateLayout();
        }
    }

    [UxmlAttribute("text-height-percentage")]
    private float _textHeightPercentage = 0.5f; 
    public float TextHeightPercentage
    {
        get => _textHeightPercentage;
        set
        {
            _textHeightPercentage = Mathf.Clamp01(value);
            UpdateLayout();
        }
    }

    // --- NEW: SPACING VARIABLES EXPOSED TO THE INSPECTOR ---

    [UxmlAttribute("text-side-padding")]
    private float _textSidePadding = 0f; // Default 0 pixels padding on outer edges
    public float TextSidePadding
    {
        get => _textSidePadding;
        set
        {
            _textSidePadding = Mathf.Max(0f, value);
            UpdateLayout();
        }
    }

    [UxmlAttribute("label-input-gap")]
    private float _labelInputGap = 0f; // Default 0 pixels spacing between label and input
    public float LabelInputGap
    {
        get => _labelInputGap;
        set
        {
            _labelInputGap = Mathf.Max(0f, value);
            UpdateLayout();
        }
    }

    public string Value
    {
        get => _inputField.value;
        set => _inputField.value = value;
    }

    public ResponsiveTextField()
    {
        // 1. Row layout orientation
        style.flexDirection = FlexDirection.Row;
        style.alignItems = Align.Center;
        
        // Zero out container defaults completely
        style.paddingTop = 0;
        style.paddingBottom = 0;
        style.paddingLeft = 0;
        style.paddingRight = 0;

        // 2. Setup Label styles and strip default margins/paddings
        _label = new Label("Label Text");
        _label.style.height = Length.Percent(100);
        _label.style.unityTextAlign = TextAnchor.MiddleLeft;
        
        _label.style.marginTop = 0;
        _label.style.marginBottom = 0;
        _label.style.marginLeft = 0;
        _label.style.marginRight = 0;
        _label.style.paddingTop = 0;
        _label.style.paddingBottom = 0;
        _label.style.paddingLeft = 0;
        _label.style.paddingRight = 0;
        _label.style.borderTopWidth = 0;
        _label.style.borderBottomWidth = 0;
        _label.style.borderLeftWidth = 0;
        _label.style.borderRightWidth = 0;

        _label.style.flexGrow = 0;                          
        _label.style.flexShrink = 1;                         
        _label.style.whiteSpace = WhiteSpace.NoWrap;       
        _label.style.overflow = Overflow.Hidden;           
        _label.style.textOverflow = TextOverflow.Ellipsis; 
        Add(_label);

        // 3. Setup Input Box and strip default margins/paddings
        _inputField = new TextField();
        _inputField.style.height = Length.Percent(100);
        _inputField.style.flexGrow = 1;                      
        _inputField.style.flexShrink = 1;
        
        _inputField.style.marginTop = 0;
        _inputField.style.marginBottom = 0;
        _inputField.style.marginLeft = 0;
        _inputField.style.marginRight = 0;
        _inputField.style.paddingTop = 0;
        _inputField.style.paddingBottom = 0;
        _inputField.style.paddingLeft = 0;
        _inputField.style.paddingRight = 0;
        _inputField.style.borderTopWidth = 0;
        _inputField.style.borderBottomWidth = 0;
        _inputField.style.borderLeftWidth = 0;
        _inputField.style.borderRightWidth = 0;
        _inputField.style.backgroundColor = Color.clear;
        
        var innerInput = _inputField.Q(className: "unity-text-field__input");
        if (innerInput != null)
        {
            innerInput.style.height = Length.Percent(100);
            innerInput.style.marginTop = 0;
            innerInput.style.marginBottom = 0;
            innerInput.style.marginLeft = 0;
            innerInput.style.marginRight = 0;
            innerInput.style.paddingTop = 0;
            innerInput.style.paddingBottom = 0;
            innerInput.style.paddingLeft = 0;
            innerInput.style.paddingRight = 0;
            innerInput.style.borderTopWidth = 0;
            innerInput.style.borderBottomWidth = 0;
            innerInput.style.borderLeftWidth = 0;
            innerInput.style.borderRightWidth = 0;
            innerInput.style.backgroundColor = Color.clear;
        }

        Add(_inputField);

        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        float totalWidth = resolvedStyle.width;
        float totalHeight = resolvedStyle.height;

        // --- APPLY DYNAMIC SPACING CONSTRAINTS ---
        
        // 1. Text Side Padding updates the main element's edge offsets
        style.paddingLeft = _textSidePadding;
        style.paddingRight = _textSidePadding;

        // 2. Label Input Gap applies space between the elements
        _label.style.marginRight = _labelInputGap;

        if (totalWidth > 0)
        {
            // Accounts for our left/right padding to calculate accurate maximum constraints
            float usableWidth = totalWidth - (_textSidePadding * 2f);
            float maxLabelWidth = usableWidth * _labelWidthPercentage;

            _label.style.width = StyleKeyword.Null; 
            _label.style.maxWidth = maxLabelWidth;
        }

        if (totalHeight > 0)
        {
            float targetFontSize = totalHeight * _textHeightPercentage;
            _label.style.fontSize = targetFontSize;
            _inputField.style.fontSize = targetFontSize;

            var innerInput = _inputField.Q(className: "unity-text-field__input");
            if (innerInput != null)
            {
                innerInput.style.fontSize = targetFontSize;
            }
        }
    }

    public void RegisterValueChangedCallback(EventCallback<ChangeEvent<string>> callback)
    {
        _inputField.RegisterValueChangedCallback(callback);
    }
}
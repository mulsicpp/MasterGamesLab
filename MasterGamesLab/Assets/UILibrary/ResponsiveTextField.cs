using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class ResponsiveTextField : VisualElement
{
    private readonly Label _label;
    private readonly TextField _inputField;
    private VisualElement _innerInput;
    private IVisualElementScheduledItem _blinkTask;
    private bool _isCursorVisible = true;

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

    // --- NEW: CURSOR CUSTOM ATTRIBUTES ---

    [UxmlAttribute("cursor-blink-rate")]
    private long _cursorBlinkRateMs = 500;
    public long CursorBlinkRateMs
    {
        get => _cursorBlinkRateMs;
        set
        {
            _cursorBlinkRateMs = (long)Mathf.Max(10, value);
            RestartBlinkScheduler();
        }
    }

    [UxmlAttribute("cursor-color")]
    private Color _cursorColor = Color.white; // Default base color for the cursor
    public Color CursorColor
    {
        get => _cursorColor;
        set
        {
            _cursorColor = value;
            ApplyCursorStyle();
        }
    }

    // --- SPACING VARIABLES EXPOSED TO THE INSPECTOR ---

    [UxmlAttribute("text-side-padding")]
    private float _textSidePadding = 0f;
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
    private float _labelInputGap = 0f;
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
        style.flexDirection = FlexDirection.Row;
        style.alignItems = Align.Center;

        style.paddingTop = 0;
        style.paddingBottom = 0;
        style.paddingLeft = 0;
        style.paddingRight = 0;

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

        _innerInput = _inputField.Q(className: "unity-text-field__input");
        if (_innerInput != null)
        {
            _innerInput.style.height = Length.Percent(100);
            _innerInput.style.marginTop = 0;
            _innerInput.style.marginBottom = 0;
            _innerInput.style.marginLeft = 0;
            _innerInput.style.marginRight = 0;
            _innerInput.style.paddingTop = 0;
            _innerInput.style.paddingBottom = 0;
            _innerInput.style.paddingLeft = 0;
            _innerInput.style.paddingRight = 0;
            _innerInput.style.borderTopWidth = 0;
            _innerInput.style.borderBottomWidth = 0;
            _innerInput.style.borderLeftWidth = 0;
            _innerInput.style.borderRightWidth = 0;
            _innerInput.style.backgroundColor = Color.clear;
        }

        Add(_inputField);

        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

        _inputField.RegisterCallback<FocusEvent>(OnFieldFocused);
        _inputField.RegisterCallback<BlurEvent>(OnFieldBlurred);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        UpdateLayout();
    }

    private void OnFieldFocused(FocusEvent evt)
    {
        if (_innerInput == null)
        {
            _innerInput = _inputField.Q(className: "unity-text-field__input");
        }

        RestartBlinkScheduler();
    }

    private void OnFieldBlurred(BlurEvent evt)
    {
        _blinkTask?.Pause();
        // Restore cursor color to default state when unfocused so it doesn't stay stuck invisible
        _isCursorVisible = true;
        ApplyCursorStyle();
    }

    private void RestartBlinkScheduler()
    {
        _blinkTask?.Pause();
        if (_innerInput == null) return;

        _blinkTask = schedule.Execute(() =>
        {
            _isCursorVisible = !_isCursorVisible;
            ApplyCursorStyle();
        }).Every(_cursorBlinkRateMs);
    }

    private void ApplyCursorStyle()
    {
        if (_inputField == null) return;

        // Direct access to the caret drawing engine properties
        Color currentBlinkVisualColor = _isCursorVisible ? _cursorColor : Color.clear;
        _inputField.textSelection.cursorColor = currentBlinkVisualColor;

    }

    public new void Focus()
    {
        Debug.Log(_inputField?.focusable);
        _inputField?.Focus();
    }

    private void UpdateLayout()
    {
        float totalWidth = resolvedStyle.width;
        float totalHeight = resolvedStyle.height;

        style.paddingLeft = _textSidePadding;
        style.paddingRight = _textSidePadding;

        _label.style.marginRight = _labelInputGap;

        if (totalWidth > 0)
        {
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

            if (_innerInput != null)
            {
                _innerInput.style.fontSize = targetFontSize;
            }
        }
    }

    public void RegisterValueChangedCallback(EventCallback<ChangeEvent<string>> callback)
    {
        _inputField.RegisterValueChangedCallback(callback);
    }
}
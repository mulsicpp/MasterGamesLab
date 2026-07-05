using UnityEngine;
using UnityEngine.UIElements;
using InGameCamera;

namespace UI
{
    public abstract class Pin : MonoBehaviour
    {
        private static readonly int OutlineColor = Shader.PropertyToID("_Color");
        private static readonly int OutlineThickness = Shader.PropertyToID("_Spread");

        public enum PinDirection
        {
            Center,
            Bottom,
            Top,
            Left,
            Right,
            BottomLeft,
            BottomRight,
            TopLeft,
            TopRight
        }

        [SerializeField] private float panelOffset = 10f;
        [SerializeField] private float invisibleThreshold = -0.1f;
        [SerializeField] protected PinDirection pivotDirection = PinDirection.Bottom;

        protected abstract float pinHeightPercent { get; }
        protected abstract float pinAspectRatio { get; }

        protected Camera mainCamera;
        protected PlanetCameraController cameraController;
        protected PinboardUi pinboard;

        public VisualElement UiElement { get; private set; }
        protected VisualElement hoverable;
        private VisualElement outlineElement;
        private Material outlineMaterial;
        private bool outlineMaterialReady;
        public bool IsHovered { get; private set; }

        [SerializeField] protected VisualTreeAsset PinTemplate;

        public float UnscaledWidth { get; private set; }
        public float UnscaledHeight { get; private set; }

        private Vector2 _managedOffset = Vector2.zero;
        private bool _isShowing = true;

        protected abstract Vector3 GetTargetWorldPosition(out Vector3 upVector);
        protected abstract void InitializeUiComponents();

        private void Awake()
        {
            mainCamera = MainCamera.Instance.GetComponentInChildren<Camera>();
            cameraController = MainCamera.Instance.GetComponentInChildren<PlanetCameraController>();
            pinboard = FindAnyObjectByType<PinboardUi>();
            UiElement = pinboard.CreatePinElement(PinTemplate, pinHeightPercent, pinAspectRatio);
        }

        protected virtual void OnEnable()
        {
            if (pinboard != null) pinboard.RegisterPin(this);
        }

        protected virtual void OnDisable()
        {
            if (pinboard != null) pinboard.UnregisterPin(this);
        }

        protected virtual void Start()
        {
            outlineElement = UiElement.Q<VisualElement>(className: "pin-outline");
            outlineElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            ApplyLayoutPivots();
            InitializeUiComponents();
            CalculateUnscaledDimensions();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            var material = outlineElement.resolvedStyle.unityMaterial.material;
            outlineMaterial = Instantiate(material);
            outlineElement.style.unityMaterial = new StyleMaterialDefinition(outlineMaterial);
            outlineMaterialReady = true;
            // SetOutlineColor(new Color(0, 0, 0, 0));
            ClearOutline();
        }

        private void CalculateUnscaledDimensions()
        {
            float containerHeight = pinboard.root.layout.height;
            if (float.IsNaN(containerHeight) || containerHeight <= 0) containerHeight = Screen.height;

            UnscaledHeight = containerHeight * (pinHeightPercent / 100f);
            UnscaledWidth = UnscaledHeight * pinAspectRatio;
        }

        public void SetOutlineColor(Color color)
        {
            if (outlineMaterialReady)
            {
                outlineMaterial.SetColor(OutlineColor, color);
                outlineMaterial.SetFloat(OutlineThickness, 0.01f);
            }
        }

        public void ClearOutline()
        {
            if (outlineMaterialReady)
            {
                outlineMaterial.SetColor(OutlineColor, new Color(0, 0, 0, 0));
                outlineMaterial.SetFloat(OutlineThickness, 0);
            }
        }

        public void SetManagedOffset(Vector2 offset)
        {
            _managedOffset = offset;
        }

        public bool IsShowing() => _isShowing;

        protected virtual void LateUpdate()
        {
            if (!_isShowing) return;

            Vector3 worldPos = GetTargetWorldPosition(out Vector3 upVector);
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            bool facingAway = Vector3.Dot((mainCamera.transform.position - worldPos).normalized, upVector) < invisibleThreshold;

            if (screenPos.z < 0 || facingAway)
            {
                UiElement.style.display = DisplayStyle.None;
                return;
            }

            Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(pinboard.root.panel, worldPos, mainCamera);
            float scaleFactor = cameraController.ScalingFactor;

            ApplyLayoutPivots();
            Vector2 pivotOffset = GetPivotOffset();

            // ZOOM BUG FIX: By keeping our layout offsets clean inside the unscaled pixel calculations,
            // multiplying everything uniformly by scaleFactor guarantees perfectly locked layouts on zoom.
            float finalXPixels = panelPosition.x + pivotOffset.x + (_managedOffset.x * scaleFactor);
            float finalYPixels = panelPosition.y + pivotOffset.y + (_managedOffset.y * scaleFactor);

            UiElement.style.translate = new StyleTranslate(new Translate(
                new Length(finalXPixels, LengthUnit.Pixel),
                new Length(finalYPixels, LengthUnit.Pixel)
            ));

            UiElement.style.scale = new StyleScale(new Scale(new Vector3(scaleFactor, scaleFactor, 1f)));
            UiElement.style.display = DisplayStyle.Flex;
        }

        private Vector2 GetPivotOffset()
        {
            return pivotDirection switch
            {
                PinDirection.Bottom => new Vector2(-(UnscaledWidth * 0.5f), -UnscaledHeight - panelOffset),
                PinDirection.Center => new Vector2(-(UnscaledWidth * 0.5f), -(UnscaledHeight * 0.5f)),
                PinDirection.Top => new Vector2(-(UnscaledWidth * 0.5f), panelOffset),
                PinDirection.Left => new Vector2(panelOffset, -(UnscaledHeight * 0.5f)),
                PinDirection.Right => new Vector2(-UnscaledWidth - panelOffset, -(UnscaledHeight * 0.5f)),
                PinDirection.BottomLeft => new Vector2(panelOffset, -UnscaledHeight - panelOffset),
                PinDirection.BottomRight => new Vector2(-UnscaledWidth - panelOffset, -UnscaledHeight - panelOffset),
                PinDirection.TopLeft => new Vector2(panelOffset, panelOffset),
                PinDirection.TopRight => new Vector2(-UnscaledWidth - panelOffset, panelOffset),
                _ => Vector2.zero
            };
        }

        private void ApplyLayoutPivots()
        {
            Length left = new Length(0, LengthUnit.Percent);
            Length center = new Length(50, LengthUnit.Percent);
            Length right = new Length(100, LengthUnit.Percent);
            Length top = new Length(0, LengthUnit.Percent);
            Length bottom = new Length(100, LengthUnit.Percent);

            var (x, y) = pivotDirection switch
            {
                PinDirection.Center => (center, center),
                PinDirection.Bottom => (center, bottom),
                PinDirection.Top => (center, top),
                PinDirection.Left => (left, center),
                PinDirection.Right => (right, center),
                PinDirection.BottomLeft => (left, bottom),
                PinDirection.BottomRight => (right, bottom),
                PinDirection.TopLeft => (left, top),
                PinDirection.TopRight => (right, top),
                _ => (center, center)
            };

            UiElement.style.transformOrigin = new StyleTransformOrigin(new TransformOrigin(x, y));
        }

        protected virtual void OnPointerEnterElement(PointerEnterEvent evt) => IsHovered = true;
        protected virtual void OnPointerLeaveElement(PointerLeaveEvent evt) => IsHovered = false;
        
        protected virtual void SetShowing(bool active)
        {
            _isShowing = active;
            UiElement.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected virtual void OnDestroy()
        {
            if (pinboard != null) pinboard.UnregisterPin(this);
            UiElement?.RemoveFromHierarchy();
        }
    }
}
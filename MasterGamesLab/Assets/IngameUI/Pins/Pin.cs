using UnityEngine;
using UnityEngine.UIElements;
using InGameCamera;
using UnityEngine.PlayerLoop;

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

        [SerializeField] protected VisualTreeAsset PinTemplate;

        public float UnscaledWidth { get; private set; }
        public float UnscaledHeight { get; private set; }

        private Vector2 _managedOffset = Vector2.zero;
        private bool _isShowing = true;

        public virtual Vector3 GetTargetWorldPosition(out Vector3 upVector)
        {
            Vector3 rawPosition = gameObject.transform.position;
            Vector3 projectedPosition = Map.Map.Instance.GetProjectedPosition(rawPosition);
            upVector = (Map.Map.Instance.GetProjectedPosition(rawPosition * 1.01f) - projectedPosition).normalized;
            return projectedPosition;
        }

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
            outlineElement.RegisterCallbackOnce<GeometryChangedEvent>(OnGeometryChanged);

            ApplyLayoutPivots();
            InitializeUiComponents();

            if (outlineElement.resolvedStyle.unityMaterial.material != null)
            {
                InitializeOutlineMaterial();
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            InitializeOutlineMaterial();
        }

        private void InitializeOutlineMaterial()
        {
            if (outlineMaterialReady) return;

            var material = outlineElement.resolvedStyle.unityMaterial.material;
            if (material == null) return;

            outlineMaterial = Instantiate(material);
            outlineElement.style.unityMaterial = new StyleMaterialDefinition(outlineMaterial);
            outlineMaterialReady = true;

            if (_pendingOutlineColor.HasValue)
            {
                SetOutline(_pendingOutlineColor.Value);
                _pendingOutlineColor = null;
            }
            else
            {
                ClearOutline();
            }
        }

        private Color? _pendingOutlineColor;

        private void CalculateUnscaledDimensions()
        {
            float containerHeight = pinboard.root.layout.height;
            Debug.Log("Containerhight: " + containerHeight);

            UnscaledHeight = containerHeight * (pinHeightPercent / 100f);
            UnscaledWidth = UnscaledHeight * pinAspectRatio;
        }

        public void SetOutline(Color color)
        {
            if (outlineMaterialReady)
            {
                outlineMaterial.SetColor(OutlineColor, color);
                outlineMaterial.SetFloat(OutlineThickness, 0.015f);
            }
            else
            {
                _pendingOutlineColor = color;
            }
        }

        public void ClearOutline()
        {
            if (outlineMaterialReady)
            {
                outlineMaterial.SetColor(OutlineColor, new Color(0, 0, 0, 0));
                outlineMaterial.SetFloat(OutlineThickness, 0);
            }
            else
            {
                _pendingOutlineColor = null;
            }
        }

        public void SetManagedOffset(Vector2 offset)
        {
            _managedOffset = offset;
        }

        public bool IsShowing() => _isShowing;

        protected virtual void LateUpdate()
        {
            CalculateUnscaledDimensions();
            if (!_isShowing) return;

            Vector3 worldPos = GetTargetWorldPosition(out Vector3 upVector);
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            bool facingAway = Vector3.Dot((mainCamera.transform.position - worldPos).normalized, upVector) <
                              invisibleThreshold;

            if (screenPos.z < 0 || facingAway)
            {
                UiElement.style.display = DisplayStyle.None;
                return;
            }

            Vector2 panelPosition =
                RuntimePanelUtils.CameraTransformWorldToPanel(pinboard.root.panel, worldPos, mainCamera);
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
                PinDirection.Bottom => new Vector2(-(UnscaledWidth * 0.5f), -UnscaledHeight),
                PinDirection.Center => new Vector2(-(UnscaledWidth * 0.5f), -(UnscaledHeight * 0.5f)),
                PinDirection.Top => new Vector2(-(UnscaledWidth * 0.5f), 0),
                PinDirection.Left => new Vector2(0, -(UnscaledHeight * 0.5f)),
                PinDirection.Right => new Vector2(-UnscaledWidth, -(UnscaledHeight * 0.5f)),
                PinDirection.BottomLeft => new Vector2(0, -UnscaledHeight),
                PinDirection.BottomRight => new Vector2(-UnscaledWidth, -UnscaledHeight),
                PinDirection.TopLeft => new Vector2(0, 0),
                PinDirection.TopRight => new Vector2(-UnscaledWidth, 0),
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
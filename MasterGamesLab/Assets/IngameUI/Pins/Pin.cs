using UnityEngine;
using UnityEngine.UIElements;
using InGameCamera;

namespace UI
{
    public abstract class Pin : MonoBehaviour
    {
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
        private Vector2 lastAppliedPosition = new Vector2(-9999f, -9999f);

        public bool IsHovered { get; private set; }

        [SerializeField] protected VisualTreeAsset PinTemplate;

        protected abstract Vector3 GetTargetWorldPosition(out Vector3 upVector);
        protected abstract void InitializeUiComponents();

        private void Awake()
        {
            mainCamera = MainCamera.Instance.GetComponentInChildren<Camera>();
            cameraController = MainCamera.Instance.GetComponentInChildren<PlanetCameraController>();

            pinboard = FindAnyObjectByType<PinboardUi>();
            UiElement = pinboard.CreatePinElement(PinTemplate, pinHeightPercent, pinAspectRatio);
        }

        protected virtual void Start()
        {
            if (hoverable != null)
            {
                hoverable.RegisterCallback<PointerEnterEvent>(OnPointerEnterElement);
                hoverable.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveElement);
            }
            ApplyLayoutPivots();
            InitializeUiComponents();
        }

        protected virtual void LateUpdate()
        {
            Vector3 worldPos = GetTargetWorldPosition(out Vector3 upVector);
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

            bool facingAway = Vector3.Dot((mainCamera.transform.position - worldPos).normalized, upVector) < invisibleThreshold;

            if (screenPos.z < 0 || facingAway)
            {
                UiElement.style.display = DisplayStyle.None;
                return;
            }

            Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                pinboard.root.panel,
                worldPos,
                mainCamera
            );

            float scaleFactor = cameraController.ScalingFactor;

            // Update transform origins dynamically if direction shifts at runtime
            ApplyLayoutPivots();

            // 1. Get the cleanly calculated layout displacement offset
            Vector2 offset = GetPivotOffset(scaleFactor);

            // 2. Combine panelPosition with the exact pixel displacements
            float finalXPixels = panelPosition.x + offset.x;
            float finalYPixels = panelPosition.y + offset.y;

            UiElement.style.translate = new StyleTranslate(new Translate(
                new Length(finalXPixels, LengthUnit.Pixel),
                new Length(finalYPixels, LengthUnit.Pixel)
            ));

            UiElement.style.scale = new StyleScale(new Scale(new Vector3(scaleFactor, scaleFactor, 1f)));
            UiElement.style.display = DisplayStyle.Flex;

            lastAppliedPosition = panelPosition;
        }

        /// <summary>
        /// Calculates matching layout displacement shifts safely using 
        /// the raw unscaled pixel sizes of your percentage layouts.
        /// </summary>
        private Vector2 GetPivotOffset(float currentScaleFactor)
        {
            // 1. Get the real unscaled pixel height of the canvas container
            float containerHeight = pinboard.root.layout.height;
            if (float.IsNaN(containerHeight) || containerHeight <= 0)
                containerHeight = Screen.height;

            // 2. Reconstruct raw dimensions before visual scale is applied
            float unscaledHeight = containerHeight * (pinHeightPercent / 100f);
            float unscaledWidth = unscaledHeight * pinAspectRatio;

            // 3. Calculate shifts based on raw dimensions.
            // Notice how we DO NOT multiply width/height by currentScaleFactor here.
            // The scale transform handles this automatically relative to the transformOrigin.
            return pivotDirection switch
            {
                PinDirection.Bottom => new Vector2(-(unscaledWidth * 0.5f), -unscaledHeight - panelOffset),
                PinDirection.Center => new Vector2(-(unscaledWidth * 0.5f), -(unscaledHeight * 0.5f)),
                PinDirection.Top => new Vector2(-(unscaledWidth * 0.5f), panelOffset),
                PinDirection.Left => new Vector2(panelOffset, -(unscaledHeight * 0.5f)),
                PinDirection.Right => new Vector2(-unscaledWidth - panelOffset, -(unscaledHeight * 0.5f)),
                PinDirection.BottomLeft => new Vector2(panelOffset, -unscaledHeight - panelOffset),
                PinDirection.BottomRight => new Vector2(-unscaledWidth - panelOffset, -unscaledHeight - panelOffset),
                PinDirection.TopLeft => new Vector2(panelOffset, panelOffset),
                PinDirection.TopRight => new Vector2(-unscaledWidth - panelOffset, panelOffset),
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
        protected virtual void SetShowing(bool active) => UiElement.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        protected virtual void OnDestroy() => UiElement?.RemoveFromHierarchy();
    }
}
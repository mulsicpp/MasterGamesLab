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
            Bottom,      // Exact same math as your original script
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
        private Vector2 lastAppliedPosition = new Vector2(-9999f, -9999f);

        public bool IsHovered { get; private set; }

        [SerializeField] protected VisualTreeAsset PinTemplate;

        protected abstract Vector3 GetTargetWorldPosition(out Vector3 upVector);
        protected abstract void InitializeUiComponents();

        protected virtual void Start()
        {
            mainCamera = MainCamera.Instance.GetComponentInChildren<Camera>();
            cameraController = MainCamera.Instance.GetComponentInChildren<PlanetCameraController>();
            pinboard = FindAnyObjectByType<PinboardUi>();

            UiElement = pinboard.CreatePinElement(PinTemplate, pinHeightPercent, pinAspectRatio);

            UiElement.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            UiElement.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement);

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

            // 1. Get the unscaled layout displacement offset
            Vector2 offset = GetPivotOffset();

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
        /// Calculates matching layout displacement shifts. 
        /// PinDirection.Bottom matches your original formula perfectly.
        /// </summary>
        private Vector2 GetPivotOffset()
        {
            float width = UiElement.layout.width;
            float height = UiElement.layout.height;

            // Fallback rules if UI Toolkit layout properties haven't resolved yet
            if (float.IsNaN(width) || width <= 0) width = 100f; 
            if (float.IsNaN(height) || height <= 0) height = 100f;

            return pivotDirection switch
            {
                // YOUR ORIGINAL FORMULA: x - (width * 0.5f), y - height - panelOffset
                PinDirection.Bottom      => new Vector2(-(width * 0.5f), -height - panelOffset),
                
                PinDirection.Center      => new Vector2(-(width * 0.5f), -(height * 0.5f)),
                PinDirection.Top         => new Vector2(-(width * 0.5f), panelOffset),
                PinDirection.Left        => new Vector2(panelOffset,     -(height * 0.5f)),
                PinDirection.Right       => new Vector2(-width - panelOffset, -(height * 0.5f)),
                PinDirection.BottomLeft  => new Vector2(panelOffset,     -height - panelOffset),
                PinDirection.BottomRight => new Vector2(-width - panelOffset, -height - panelOffset),
                PinDirection.TopLeft     => new Vector2(panelOffset,     panelOffset),
                PinDirection.TopRight    => new Vector2(-width - panelOffset, panelOffset),
                _                        => Vector2.zero
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
                PinDirection.Center      => (center, center),
                PinDirection.Bottom      => (center, bottom),
                PinDirection.Top         => (center, top),
                PinDirection.Left        => (left,   center),
                PinDirection.Right       => (right,  center),
                PinDirection.BottomLeft  => (left,   bottom),
                PinDirection.BottomRight => (right,  bottom),
                PinDirection.TopLeft     => (left,   top),
                PinDirection.TopRight    => (right,  top),
                _                        => (center, center)
            };

            UiElement.style.transformOrigin = new StyleTransformOrigin(new TransformOrigin(x, y));
        }

        protected virtual void OnMouseEnterElement(MouseEnterEvent evt) => IsHovered = true;
        protected virtual void OnMouseLeaveElement(MouseLeaveEvent evt) => IsHovered = false;
        protected virtual void setActive(bool active) => UiElement.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        protected virtual void OnDestroy() => UiElement?.RemoveFromHierarchy();
    }
}
using UnityEngine;
using UnityEngine.UIElements;
using InGameCamera;

namespace UI
{
    public abstract class Pin : MonoBehaviour
    {
        [SerializeField] private float panelOffset = 10f;
        [SerializeField] private float invisibleThreshold = -0.1f;

        protected Camera mainCamera;
        protected PlanetCameraController cameraController;
        protected PinboardUi pinboard;

        public VisualElement UiElement { get; private set; }
        private Vector2 lastAppliedPosition = new Vector2(-9999f, -9999f);

        public bool IsHovered { get; private set; }

        // Abstract definitions children MUST provide
        [SerializeField] protected VisualTreeAsset PinTemplate;

        protected abstract Vector3 GetTargetWorldPosition(out Vector3 upVector);
        protected abstract void InitializeUiComponents();

        protected virtual void Start()
        {
            mainCamera = MainCamera.Instance.GetComponentInChildren<Camera>();
            cameraController = MainCamera.Instance.GetComponentInChildren<PlanetCameraController>();
            pinboard = FindAnyObjectByType<PinboardUi>();

            UiElement = pinboard.CreatePinElement(PinTemplate);

            UiElement.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            UiElement.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement);

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

            // CLEAN: No more layout.width or layout.height checking! 
            // Because of wrapper.style.translate, this positions the CENTER of the UI directly on the world point.
            Vector2 targetPosition = new Vector2(
                panelPosition.x,
                panelPosition.y - panelOffset
            );

            UiElement.style.scale = new StyleScale(new Scale(new Vector3(cameraController.ScalingFactor, cameraController.ScalingFactor, 1f)));
            UiElement.style.left = targetPosition.x;
            UiElement.style.top = targetPosition.y;
            UiElement.style.display = DisplayStyle.Flex;

            lastAppliedPosition = targetPosition;
        }

        protected virtual void OnMouseEnterElement(MouseEnterEvent evt)
        {
            IsHovered = true;
        }

        protected virtual void OnMouseLeaveElement(MouseLeaveEvent evt)
        {
            IsHovered = false;
        }
        protected virtual void setActive(bool active)
        {
            UiElement.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected virtual void OnDestroy()
        {
            UiElement?.RemoveFromHierarchy();
        }

    }
}
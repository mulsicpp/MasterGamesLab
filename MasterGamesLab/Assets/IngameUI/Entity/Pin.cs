using UnityEngine;
using UnityEngine.UIElements;
using InGameCamera;

namespace UI
{
    public abstract class Pin : MonoBehaviour
    {
        [SerializeField] private float panelOffset = 10f;
        [SerializeField] private float invisibleThreshold = -0.1f;

        [Header("Optimization")]
        [SerializeField] private float positionEpsilon = 0.5f;

        protected Camera mainCamera;
        protected PlanetCameraController cameraController;
        protected PinboardUi pinboard;

        public VisualElement UiElement { get; private set; }
        private Vector2 lastAppliedPosition = new Vector2(-9999f, -9999f);

        // Abstract definitions children MUST provide
        protected abstract VisualTreeAsset PinTemplate { get; }
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

            float halfWidth = UiElement.layout.width / 2f;
            float halfHeight = UiElement.layout.height / 2f;

            Vector2 targetLeftTop = new Vector2(
                panelPosition.x - halfWidth,
                panelPosition.y - halfHeight - panelOffset
            );

            if (UiElement.style.display == DisplayStyle.Flex &&
                Vector2.SqrMagnitude(targetLeftTop - lastAppliedPosition) < (positionEpsilon * positionEpsilon))
            {
                return;
            }

            UiElement.style.scale = new StyleScale(new Scale(new Vector3(cameraController.ScalingFactor, cameraController.ScalingFactor, 1f)));
            UiElement.style.left = targetLeftTop.x;
            UiElement.style.top = targetLeftTop.y;
            UiElement.style.display = DisplayStyle.Flex;

            lastAppliedPosition = targetLeftTop;
        }

        protected virtual void OnMouseEnterElement(MouseEnterEvent evt)
        {
            Map.Map.Instance.isOverUI = true;
            Map.Map.Instance.CurrentlyHovered = null;
        }

        protected virtual void OnMouseLeaveElement(MouseLeaveEvent evt)
        {
            Map.Map.Instance.isOverUI = false;
        }

        protected virtual void OnDestroy()
        {
            UiElement?.RemoveFromHierarchy();
        }
    }
}
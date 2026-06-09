using UnityEngine;
using UnityEngine.UIElements;
using UI;
using InGameCamera;
using Unity.VisualScripting; // Matches your PinboardUi namespace

public class PinUI : MonoBehaviour
{
    [SerializeField] private float panelOffset = 10f;
    [SerializeField] private float invisibleTreshhold = -0.1f;

    private Camera mainCamera;
    private PlanetCameraController cameraController;

    private VisualElement myUiElement;
    PinboardUi pinboard;
    Transform meshTransform;

    void Start()
    {
        mainCamera = MainCamera.Instance.GetComponentInChildren<Camera>();
        cameraController = MainCamera.Instance.GetComponentInChildren<PlanetCameraController>();

        pinboard = FindAnyObjectByType<PinboardUi>();

        myUiElement = pinboard.CreateTruckIndicator();

        meshTransform = GetComponentInChildren<MeshRenderer>().transform;

    }

    void LateUpdate()
    {
        Vector3 targetWorldPosition = meshTransform.position;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(targetWorldPosition);

        bool facingAway = Vector3.Dot((mainCamera.transform.position - targetWorldPosition).normalized, targetWorldPosition.normalized) < invisibleTreshhold;

        if (screenPos.z < 0 || facingAway)
        {
            myUiElement.style.display = DisplayStyle.None;
            return;
        }

        Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
            pinboard.root.panel,
            targetWorldPosition,
            mainCamera
        );


        myUiElement.style.scale = new StyleScale(new Scale(new Vector3(cameraController.ScalingFactor, cameraController.ScalingFactor, 1f)));

        float halfWidth = myUiElement.layout.width / 2f;
        float halfHeight = myUiElement.layout.height / 2f;

        myUiElement.style.left = panelPosition.x - halfWidth;
        myUiElement.style.top = panelPosition.y - halfHeight - panelOffset;
        myUiElement.style.display = DisplayStyle.Flex;
    }

    private void OnDestroy()
    {
        myUiElement?.RemoveFromHierarchy();
    }
}
using UnityEngine;
using UnityEngine.UIElements;
using UI;
using InGameCamera;
using Map.GeometryGeneration;
using Map.Fleet;

public class PinUI : MonoBehaviour
{
    [SerializeField] private float panelOffset = 10f;
    [SerializeField] private float invisibleTreshhold = -0.1f;
    
    [Header("Optimization")]
    [SerializeField] private float positionEpsilon = 0.5f;

    private Camera mainCamera;
    private PlanetCameraController cameraController;

    private VisualElement myUiElement;
    private PinboardUi pinboard;
    private VehicleRenderer vehicleRenderer;

    private Vector2 lastAppliedPosition = new Vector2(-9999f, -9999f);

    void Start()
    {
        mainCamera = MainCamera.Instance.GetComponentInChildren<Camera>();
        cameraController = MainCamera.Instance.GetComponentInChildren<PlanetCameraController>();

        pinboard = FindAnyObjectByType<PinboardUi>();

        myUiElement = pinboard.CreateTruckIndicator();

        vehicleRenderer = GetComponent<VehicleRenderer>();
    }

    void LateUpdate()
    {
        VehicleTransform vehicleTransform = Map.Map.Instance.GetProjectedVehicleTransform(vehicleRenderer.Vehicle.Transform);

        Vector3 screenPos = mainCamera.WorldToScreenPoint(vehicleTransform.Position);

        bool facingAway = Vector3.Dot((mainCamera.transform.position - vehicleTransform.Position).normalized, vehicleTransform.Up) < invisibleTreshhold;

        if (screenPos.z < 0 || facingAway)
        {
            myUiElement.style.display = DisplayStyle.None;
            return;
        }

        Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
            pinboard.root.panel,
            vehicleTransform.Position,
            mainCamera
        );

        float halfWidth = myUiElement.layout.width / 2f;
        float halfHeight = myUiElement.layout.height / 2f;

        Vector2 targetLeftTop = new Vector2(
            panelPosition.x - halfWidth,
            panelPosition.y - halfHeight - panelOffset
        );

        if (myUiElement.style.display == DisplayStyle.Flex && 
            Vector2.SqrMagnitude(targetLeftTop - lastAppliedPosition) < (positionEpsilon * positionEpsilon))
        {
            return;
        }

        myUiElement.style.scale = new StyleScale(new Scale(new Vector3(cameraController.ScalingFactor, cameraController.ScalingFactor, 1f)));
        myUiElement.style.left = targetLeftTop.x;
        myUiElement.style.top = targetLeftTop.y;
        myUiElement.style.display = DisplayStyle.Flex;

        lastAppliedPosition = targetLeftTop;
    }

    private void OnDestroy()
    {
        myUiElement?.RemoveFromHierarchy();
    }
}
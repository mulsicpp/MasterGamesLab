using UnityEngine;
using UnityEngine.UIElements;
using UI; // Matches your PinboardUi namespace

public class PinUI : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 2.5f, 0); // Height above the truck mesh

    private Camera mainCamera;
    private VisualElement myUiElement;
    private Label timeLabel;

    void Start()
    {
        mainCamera = Camera.main;

        PinboardUi pinboard = FindAnyObjectByType<PinboardUi>();

        myUiElement = pinboard.CreateTruckIndicator();
        
        timeLabel = myUiElement.Q<Label>("TimeLabel");
    }

    void LateUpdate()
    {
        if (myUiElement == null || mainCamera == null) return;

        Vector3 targetWorldPosition = transform.position + worldOffset;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(targetWorldPosition);
        if (screenPos.z < 0)
        {
            myUiElement.style.display = DisplayStyle.None;
            return;
        }

        Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
            myUiElement.panel, 
            targetWorldPosition, 
            mainCamera
        );

        float halfWidth = myUiElement.layout.width / 2f;
        float halfHeight = myUiElement.layout.height / 2f;

        myUiElement.style.left = panelPosition.x - halfWidth;
        myUiElement.style.top = panelPosition.y - halfHeight;
        myUiElement.style.display = DisplayStyle.Flex;
    }

    public void UpdateMyTimer(string newTime)
    {
        if (timeLabel != null) timeLabel.text = newTime;
    }

    private void OnDestroy()
    {
        myUiElement?.RemoveFromHierarchy();
    }
}
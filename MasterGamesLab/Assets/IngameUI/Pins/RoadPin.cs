using UI;
using UnityEngine;
using UnityEngine.UIElements;

public class RoadPin : Pin
{
    private Label cost, duration;
    private VisualElement arrow;
    RouteRenderer routeRenderer;

    protected override float pinHeightPercent => 6f;

    protected override float pinAspectRatio => 4.5f;

    public bool FacingLeft = false;


    public void OnEnable()
    {
        routeRenderer = GetComponentInParent<RouteRenderer>();
    }

    private void Update()
    {
        pivotDirection = FacingLeft ? PinDirection.Left : PinDirection.Right;
        arrow.style.scale = new StyleScale(new Scale(new Vector3(FacingLeft ? -1 : 1, 1, 1)));
    }

    protected override void LateUpdate()
    {
        if (routeRenderer.Route.TileIds == null)
        {
            setActive(false);
            return;
        }
        setActive(true);
        base.LateUpdate();
    }


    protected override Vector3 GetTargetWorldPosition(out Vector3 upVector)
    {
        Vector3 rawPosition = gameObject.transform.position;
        Vector3 projectedPosition = Map.Map.Instance.GetProjectedPosition(rawPosition);
        upVector = (Map.Map.Instance.GetProjectedPosition(rawPosition * 1.01f) - projectedPosition).normalized;
        return projectedPosition;
    }

    protected override void InitializeUiComponents()
    {
        cost = UiElement.Q<Label>("CostLabel");
        duration = UiElement.Q<Label>("DurationLabel");
        arrow = UiElement.Q<VisualElement>("Arrow");
    }
}

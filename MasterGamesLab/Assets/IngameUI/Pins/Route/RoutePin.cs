using Map.Hoverables;
using UI;
using UnityEngine;
using UnityEngine.UIElements;

public class RoutePin : Pin
{
    private Label cost, duration;
    private VisualElement arrow, element;
    private RouteRenderer routeRenderer;

    protected override float pinHeightPercent => 6f;
    protected override float pinAspectRatio => 4.5f;

    public bool FacingLeft = false;

    protected override void Start()
    {
        hoverable = UiElement.Q<VisualElement>("Pickable");
        base.Start();
        hoverable.BringToFront();
    }

    public void OnEnable()
    {
        routeRenderer = GetComponentInParent<RouteRenderer>();
    }

    private void Update()
    {
        // Dynamically configure tracking anchors
        pivotDirection = FacingLeft ? PinDirection.Right : PinDirection.Left;

        if (IsHovered)
        {
            Map.Map.Instance.CurrentlyHovered = routeRenderer.Geometry;
            HoverablePicker.Instance.DenyPick = true;
        }

        if (arrow != null)
        {
            arrow.style.scale = new StyleScale(new Scale(new Vector3(FacingLeft ? 1f : -1f, 1f, 1f)));
            element.style.flexDirection = FacingLeft ? FlexDirection.Row : FlexDirection.RowReverse;
        }
    }

    protected override void LateUpdate()
    {
        if (routeRenderer == null || !routeRenderer.PinVisible || routeRenderer.Route.TileIds == null)
        {
            SetShowing(false);
            return;
        }
        cost.text = routeRenderer.Route.Cost.ToString();
        duration.text = ((int)Mathf.Ceil(routeRenderer.Route.Duration)).ToString();
        SetShowing(true);
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
        element = UiElement.Q<VisualElement>("Element");
    }
}
using Map.Hoverables;
using UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class RoutePin : Pin
    {
        private Label cost, duration;
        private VisualElement arrow, element;
        private RouteRenderer routeRenderer;

        protected override float pinHeightPercent => 3f;
        protected override float pinAspectRatio => 4.5f;

        public bool FacingLeft = false;

        // MANAGER HOOKS: Routes span across tiles, so returning null 
        // bypasses the side-by-side stacking layout logic entirely.

        protected override void OnEnable()
        {
            routeRenderer = GetComponentInParent<RouteRenderer>();
            base.OnEnable(); // Registers to the manager pool cleanly
        }

        protected override void Start()
        {
            hoverable = UiElement.Q<VisualElement>("Pickable");
            base.Start();
        }

        private void Update()
        {
            hoverable.userData = routeRenderer.Geometry;

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

            SetShowing(true);
            cost.text = routeRenderer.Route.Cost.ToString();
            duration.text = routeRenderer.Route.Duration.ToString();

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
            element = UiElement.Q<VisualElement>("Element");
        }
    }
}
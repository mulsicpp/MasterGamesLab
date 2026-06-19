using UnityEngine;
using UnityEngine.UIElements;
using Map.Fleet;
using Map.Hoverables;

namespace UI
{
    public class VehiclePin : Pin
    {

        [SerializeField] VectorImage icon;
        private VehicleRenderer vehicleRenderer;

        private Label time;

        protected override float pinHeightPercent => 6f;
        protected override float pinAspectRatio => 0.5f;

        public void OnEnable()
        {
            vehicleRenderer = GetComponentInParent<VehicleRenderer>();

        }

        private void Update()
        {
            if (IsHovered && Map.Map.Instance.HoverLayers.HasFlag(HoverablePicker.HoverableLayer.Vehicles))
            {
                Map.Map.Instance.CurrentlyHovered = vehicleRenderer.Vehicle;
                HoverablePicker.Instance.DenyPick = true;
            }
            if (vehicleRenderer.Vehicle.IsParked)
            {
                time.style.visibility = Visibility.Hidden;
            }
            else
            {
                time.style.visibility = Visibility.Visible;
            }
        }

        protected override void LateUpdate()
        {
            if (!vehicleRenderer.Vehicle.Exists || vehicleRenderer.Vehicle.Transform == null)
            {
                setActive(false);
                return;
            }
            time.text = vehicleRenderer.Vehicle.RemainingDriveTime is float t ? ((int)Mathf.Ceil(t)).ToString() + "s" : "";
            base.LateUpdate();
        }

        protected override Vector3 GetTargetWorldPosition(out Vector3 upVector)
        {
            Vector3 rawPosition = gameObject.transform.position;
            Vector3 projectedPosition = Map.Map.Instance.GetProjectedPosition(rawPosition);
            upVector = Map.Map.Instance.GetProjectedVehicleTransform(vehicleRenderer.Vehicle.Transform).Up;
            return projectedPosition;
        }

        protected override void InitializeUiComponents()
        {
            UiElement.Q<VisualElement>("Icon").style.backgroundImage = new StyleBackground(icon);
            time = UiElement.Q<Label>("TimeLabel");
        }

        // override protected void OnMouseEnterElement(MouseEnterEvent evt)
        // {
        //     if (!Map.Map.Instance.HoverLayers.HasFlag(HoverablePicker.HoverableLayer.Vehicles)) return;
        //     Map.Map.Instance.isOverUI = true;
        //     Map.Map.Instance.CurrentlyHovered = vehicleRenderer.Vehicle;
        // }
    }
}
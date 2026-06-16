using UnityEngine;
using UnityEngine.UIElements;
using Map.Fleet;

namespace UI
{
    public class VehiclePin : Pin
    {
        private VehicleRenderer vehicleRenderer;
        private Button pinButton;

        protected override VisualTreeAsset PinTemplate => PinboardUi.Instance.truckTemplate;

        public void AssignRenderer(VehicleRenderer renderer)
        {
            vehicleRenderer = renderer;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (!vehicleRenderer.Vehicle.Exists)
                setActive(false);
        }

        protected override Vector3 GetTargetWorldPosition(out Vector3 upVector)
        {
            var projectedTransform = Map.Map.Instance.GetProjectedVehicleTransform(vehicleRenderer.Vehicle.Transform);
            upVector = projectedTransform.Up;
            return projectedTransform.Position;
        }

        protected override void InitializeUiComponents()
        {

        }

        override protected void OnMouseEnterElement(MouseEnterEvent evt)
        {
            Map.Map.Instance.isOverUI = true;
            Map.Map.Instance.CurrentlyHovered = vehicleRenderer.Vehicle;
        }
    }
}
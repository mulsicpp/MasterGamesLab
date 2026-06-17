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
        private Button pinButton;

        public void OnEnable()
        {
            vehicleRenderer = GetComponent<VehicleRenderer>();
        }

        private void Update()
        {
            if (IsHovered && Map.Map.Instance.HoverLayers.HasFlag(HoverablePicker.HoverableLayer.Vehicles))
            {
                Map.Map.Instance.isOverUI = true;
                Map.Map.Instance.CurrentlyHovered = vehicleRenderer.Vehicle;
            } else
            {
                Map.Map.Instance.isOverUI = false;
            }
        }

        protected override void LateUpdate()
        {
            if (!vehicleRenderer.Vehicle.Exists || vehicleRenderer.Vehicle.Transform == null)
            {
                setActive(false);
                return;
            }
            base.LateUpdate();
        }

        protected override Vector3 GetTargetWorldPosition(out Vector3 upVector)
        {
            var projectedTransform = Map.Map.Instance.GetProjectedVehicleTransform(vehicleRenderer.Vehicle.Transform);
            upVector = projectedTransform.Up;
            return projectedTransform.Position;
        }

        protected override void InitializeUiComponents()
        {
            UiElement.Q<VisualElement>("Icon").style.backgroundImage = new StyleBackground(icon);
        }

        // override protected void OnMouseEnterElement(MouseEnterEvent evt)
        // {
        //     if (!Map.Map.Instance.HoverLayers.HasFlag(HoverablePicker.HoverableLayer.Vehicles)) return;
        //     Map.Map.Instance.isOverUI = true;
        //     Map.Map.Instance.CurrentlyHovered = vehicleRenderer.Vehicle;
        // }
    }
}
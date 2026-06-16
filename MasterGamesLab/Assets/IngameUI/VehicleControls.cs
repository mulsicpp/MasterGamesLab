using Map;
using Map.Fleet;
using Map.Hoverables;
using UnityEngine;
using UnityEngine.InputSystem;
using static ConstructionControls;
using static Map.Edge;

namespace UI
{
    public class VehicleControls : MonoBehaviour, IClickEventHandler, IControls
    {
        public bool ControlsAreActive => SelectedVehicle != null;

        private Vehicle selectedVehicle = null;
        public Vehicle SelectedVehicle
        {
            get { return selectedVehicle; }
            set
            {
                if (selectedVehicle != null)
                    selectedVehicle?.ClearOutline();
                selectedVehicle = value;

                if (ControlsAreActive)
                {
                    IngameUI.Instance.ConstructionControls.DisableControls();
                }
            }
        }

        public void DisableControls()
        {
            SelectedVehicle = null;
        }

        public HoverablePicker.HoverableLayer SelectHoverableLayers() => HoverablePicker.HoverableLayer.All;

        public void UpdateControls()
        {
            if(SelectedVehicle != null)
            {
                selectedVehicle.ShowOutline(Constants.SELECTED_OUTLINE);
            }
        }


        public bool HandleClick(ClickEventType type)
        {
            switch (type)
            {
                case ClickEventType.Select:
                    switch (Map.Map.Instance.CurrentlyHovered)
                    {
                        case Vehicle vehicle:
                            SelectedVehicle = vehicle;
                            return true;
                        case Tile tile:
                            if (SelectedVehicle == null || !SelectedVehicle.IsParked) return false;
                            var movementProfile = SelectedVehicle.Type == Vehicle.VehicleType.Truck ? MovementProfileRegistry.TruckFastestRoute : MovementProfileRegistry.FreighterFastestRoute;
                            var tileIds = Pathfinding.FindPath(SelectedVehicle.ParkedTile, tile, movementProfile);
                            if (tileIds == null) return false;
                            Map.Map.Instance.RequestVehicleRouteServerRpc(SelectedVehicle.IndexInVehicles, tileIds);
                            SelectedVehicle = null;
                            return true;
                        default: return false;
                    }
                case ClickEventType.CancelPressed:
                    if (SelectedVehicle == null) return false;
                    SelectedVehicle = null;
                    return true;
            }
            return false;
        }
    }
}
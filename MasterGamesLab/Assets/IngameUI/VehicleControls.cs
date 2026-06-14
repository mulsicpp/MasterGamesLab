using Map.Fleet;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI
{
    public class VehicleControls : MonoBehaviour, IClickEventHandler
    {
        private Vehicle selectedVehicle = null;
        public Vehicle SelectedVehicle
        {
            get { return selectedVehicle; }
            set
            {
                if (selectedVehicle != null)
                    selectedVehicle?.ClearOutline();
                selectedVehicle = value;
            }
        }

        public void Update()
        {
            
        }

        public bool HandleClick(ClickEventType type)
        {
            return false;
        }
    }
}
using Map.GeometryGeneration;
using UnityEngine;
using UI;
using UnityEngine.UIElements;

namespace Map.Fleet
{
    public class FreighterRenderer : VehicleRenderer
    {
        public FreighterPin Pin { get; private set; }
        protected override void InitVehicle(Vehicle vehicle)
        {
            Pin = gameObject.GetComponent<FreighterPin>();

            base.InitVehicle(vehicle);
        }
    }
}
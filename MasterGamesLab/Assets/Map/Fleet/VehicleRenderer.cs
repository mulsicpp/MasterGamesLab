using Map.GeometryGeneration;
using UnityEngine;

namespace Map.Fleet
{
    public class VehicleRenderer : MonoBehaviour
    {

        public Vehicle Vehicle { get; private set; }
        public PinUI Pin { get; private set; }
        public ObjectWithFixedGeometry Geometry { get; private set; }

        public void Init(Vehicle vehicle)
        {
            Vehicle = vehicle;
            Geometry = vehicle.AttachVehicleGeometry(transform);
        }
    }
}
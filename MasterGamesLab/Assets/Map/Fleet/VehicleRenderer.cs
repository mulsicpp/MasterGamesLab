using Map.GeometryGeneration;
using UnityEngine;
using UI;
using UnityEngine.UIElements;

namespace Map.Fleet
{
    public class VehicleRenderer : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset truckUiTemplate;

        public Vehicle Vehicle { get; private set; }
        public VehiclePin Pin { get; private set; }
        public ObjectWithFixedGeometry Geometry { get; private set; }

        public void Init(Vehicle vehicle)
        {

            Vehicle = vehicle;
            Pin = gameObject.AddComponent<VehiclePin>();

            Pin.AssignRenderer(this);

            Geometry = vehicle.AttachVehicleGeometry(transform);
        }
    }
}
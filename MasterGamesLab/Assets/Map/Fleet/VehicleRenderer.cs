using Map.GeometryGeneration;
using UnityEngine;
using UI;
using UnityEngine.UIElements;

namespace Map.Fleet
{
    public class VehicleRenderer : MonoBehaviour
    {
        public Vehicle Vehicle { get; private set; }
        public VehiclePin Pin { get; private set; }
        public ObjectWithFixedGeometry Geometry { get; private set; }
        public Transform CargoTransform;

        public void Init(Vehicle vehicle)
        {

            Vehicle = vehicle;
            Pin = gameObject.GetComponent<VehiclePin>();

            Geometry = vehicle.AttachVehicleGeometry(transform);
            Update();
        }

        public void Update()
        {
            if (Vehicle is Truck truck)
            {
                if (truck.Freighter != null)
                {
                    transform.parent = truck.Freighter.Renderer.CargoTransform;
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = Vector3.one;
                    return;
                }
            }

            transform.parent = Map.Instance.transform;

            var t = Vehicle.Transform;
            if (t == null)
            {
                Geometry.gameObject.SetActive(false);
                return;
            }

            Geometry.gameObject.SetActive(true);
            var tProj = t; // Map.Instance.GetProjectedVehicleTransform(t);
            transform.localPosition = tProj.Position;
            transform.localRotation = Quaternion.LookRotation(tProj.Forward, tProj.Up);
            transform.localScale = GeometriesManager.Scale;
        }
    }
}
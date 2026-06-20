using Map.GeometryGeneration;
using UnityEngine;
using UI;
using UnityEngine.UIElements;

namespace Map.Fleet
{
    public abstract class VehicleRenderer : MonoBehaviour
    {
        public Vehicle Vehicle { get; private set; }
        public ObjectWithFixedGeometry Geometry { get; private set; }
        public Transform CargoTransform;

        public void Init(Vehicle vehicle)
        {
            InitVehicle(vehicle);
            Update();
        }

        protected virtual void InitVehicle(Vehicle vehicle)
        {
            Vehicle = vehicle;

            Geometry = vehicle.AttachVehicleGeometry(transform);
        }

        public virtual void Update()
        {
            var t = Vehicle.Transform;
            if (t == null)
            {
                Geometry.gameObject.SetActive(false);
                return;
            }

            Geometry.gameObject.SetActive(true);

            SetVisibleOutline(Vehicle.Outline);

            var tProj = t; // Map.Instance.GetProjectedVehicleTransform(t);
            transform.localPosition = tProj.Position;
            transform.localRotation = Quaternion.LookRotation(tProj.Forward, tProj.Up);
            transform.localScale = GeometriesManager.Scale;
        }

        public virtual void SetVisibleOutline(Constants.OutlineData? outline)
        {
            if (outline is Constants.OutlineData o)
            {
                Geometry.SetOutlineLayer();
                Geometry.SetOutlineParameters(o);
            }
            else
            {
                Geometry.SetBaseLayer();
            }
        }
    }
}
using Map.GeometryGeneration;
using UnityEngine;
using UI;
using UnityEngine.UIElements;

namespace Map.Fleet
{
    public class TruckRenderer : VehicleRenderer
    {
        public ObjectWithFixedGeometry CargoGeometry { get; private set; }
        public Truck Truck { get; private set; }
        public TruckPin Pin { get; private set; }

        protected override void InitVehicle(Vehicle vehicle)
        {
            Pin = gameObject.GetComponent<TruckPin>();

            base.InitVehicle(vehicle);
            if (Vehicle is Truck t)
                Truck = t;
            else
                throw new System.ArgumentException("Vehicle needs to be a truck");

        }

        public override void Update()
        {
            if (Truck.Freighter != null)
            {
                transform.parent = Truck.Freighter.Renderer.CargoTransform;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;

                // Geometry.UpdateId(Truck.Freighter.EntityId.Value);
                // SetVisibleOutline(Truck.Freighter.Outline);

                return;
            }

            Geometry.UpdateId(Truck.EntityId.Value);
            transform.parent = Map.Instance.transform;

            base.Update();
        }

        public override void SetVisibleOutline(Constants.OutlineData? outline)
        {
            base.SetVisibleOutline(outline);
            if (outline is Constants.OutlineData o)
            {
                CargoGeometry?.SetOutlineLayer();
                CargoGeometry?.SetOutlineParameters(o);
            }
            else
            {
                CargoGeometry?.SetBaseLayer();
            }
        }
    }
}
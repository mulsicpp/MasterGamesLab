using Map.GeometryGeneration;
using UnityEngine;
using UI;
using UnityEngine.UIElements;
using Map.Infrastructure;

namespace Map.Fleet
{
    public class TruckRenderer : VehicleRenderer
    {
        public ObjectWithFixedGeometry CargoGeometry { get; private set; }
        public Truck Truck { get; private set; }
        public TruckPin Pin { get; private set; }

        private Good currentGoodMesh;

        protected override void InitVehicle(Vehicle vehicle)
        {
            Pin = gameObject.GetComponentInChildren<TruckPin>();

            base.InitVehicle(vehicle);
            currentGoodMesh = Good.None;

            if (Vehicle is Truck t)
                Truck = t;
            else
                throw new System.ArgumentException("Vehicle needs to be a truck");

        }

        public override void Update()
        {
            if (currentGoodMesh != Truck.Good)
            {
                if (CargoGeometry != null)
                {
                    Destroy(CargoGeometry.gameObject);
                    CargoGeometry = null;
                }

                if (Truck.Good != Good.None)
                {
                    var geometryType = Truck.Good switch
                    {
                        Good.Common => GeometriesManager.GeometryType.Tetrahedron,
                        Good.Uncommon => GeometriesManager.GeometryType.Cube,
                        Good.Rare => GeometriesManager.GeometryType.Octahedron,
                        Good.Epic => GeometriesManager.GeometryType.Icosahedron,
                        _ => GeometriesManager.GeometryType.Dodecahedron,
                    };

                    var scale = CargoTransform.localScale;
                    CargoGeometry = GeometriesManager.Instance.GetGameObjectGeometry(geometryType, Truck.EntityId, CargoTransform);
                    CargoTransform.localScale = scale;
                }
                currentGoodMesh = Truck.Good;
            }

            if (Truck.Freighter != null)
            {
                transform.parent = Truck.Freighter.Renderer.CargoTransform;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;

                SetVisibleOutline(Truck.Outline);

                return;
            }
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
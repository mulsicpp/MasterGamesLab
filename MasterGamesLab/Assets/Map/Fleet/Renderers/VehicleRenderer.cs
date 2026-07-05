using Map.Blueprint;
using Map.GeometryGeneration;
using UnityEngine;
using UI;

namespace Map.Fleet
{
    public abstract class VehicleRenderer : MonoBehaviour
    {
        public Vehicle Vehicle { get; private set; }
        public ObjectWithFixedGeometry Geometry { get; private set; }
        public Transform CargoTransform;
        public Pin VehiclePin;
        
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
            transform.localScale = GeometriesManager.Scale * tProj.Scale;
        }

        public virtual void SetVisibleOutline(Constants.OutlineData? outline)
        {
            if (outline is Constants.OutlineData o)
            {
                Geometry.SetOutlineLayer();
                Geometry.SetOutlineParameters(o);
                o.outlineColor.a = 1.0f;
                VehiclePin.SetOutline(o.outlineColor);
            }
            else
            {
                VehiclePin.ClearOutline();
                if (Vehicle.BlueprintTile != null)
                {
                    switch (Vehicle.BlueprintVisualState)
                    {
                        case VisualState.Preview:
                            Geometry.SetAsPreview();
                            break;
                        case VisualState.Valid:
                            Geometry.SetAsBlueprint();
                            break;
                        case VisualState.Invalid:
                            Geometry.SetAsBluePrintInvalid();
                            break;
                        case VisualState.Overlapping:
                            Geometry.SetAsBluePrintOverlapping();
                            break;
                        case VisualState.PreviewOverlapping:
                            Geometry.SetAsPreviewOverlapping();
                            break;
                        default:
                            Geometry.SetBaseLayer();
                            break;
                    }
                }
                else
                {
                    Geometry.SetBaseLayer();
                }
            }
        }
    }
}
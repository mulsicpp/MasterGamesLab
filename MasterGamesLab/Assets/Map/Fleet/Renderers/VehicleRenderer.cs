using System;
using Map.Blueprint;
using Map.GeometryGeneration;
using UnityEngine;
using UI;
using UnityEngine.UIElements;
using Map.Infrastructure;

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

            UpdateMaterial(Vehicle.Outline);

            var tProj = t; // Map.Instance.GetProjectedVehicleTransform(t);
            transform.localPosition = tProj.Position;
            transform.localRotation = Quaternion.LookRotation(tProj.Forward, tProj.Up);
            transform.localScale = GeometriesManager.Scale * tProj.Scale;
        }

        public virtual void UpdateMaterial(Constants.OutlineData? outline)
        {

            if (Vehicle.BlueprintTile != null)
            {
                switch (Vehicle.BlueprintVisualState)
                {
                    case VisualState.Preview: Geometry.SetAsPreview(); break;
                    case VisualState.PreviewOverlapping: Geometry.SetAsPreviewOverlapping(); break;
                    case VisualState.Valid: Geometry.SetAsBlueprint(); break;
                    case VisualState.Invalid: Geometry.SetAsBluePrintInvalid(); break;
                    case VisualState.Overlapping: Geometry.SetAsBluePrintOverlapping(); break;
                }
            }
            else
            {
                Geometry.SetAsBase();
            }

            if (outline is Constants.OutlineData o)
            {
                Geometry.SetOutlineLayer();
                Geometry.SetOutlineParameters(o);
            }
        }
    }
}
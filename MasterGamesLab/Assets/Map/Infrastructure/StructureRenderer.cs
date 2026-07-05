using Map.GeometryGeneration;
using UnityEngine;

namespace Map.Infrastructure
{
    public class StructureRenderer : MonoBehaviour
    {
        public Structure Structure { get; private set; }

        // public PinUI Pin { get; private set; }
        public ObjectWithFixedGeometry Geometry { get; private set; }

        public void Init(Structure structure)
        {
            Structure = structure;
            // Pin = gameObject.AddComponent<PinUI>();
            Geometry = structure.AttachStructureGeometry(transform);

            var tile = structure.Tile ?? structure.BlueprintTile;

            var position = tile.PositionOnSphere;
            var up = position.normalized;
            var forward = (tile.NeighborTiles[0].LeftVertex - tile.PositionOnSphere).normalized;

            transform.position = position;
            transform.rotation = Quaternion.LookRotation(forward, up);

            UpdateMaterial();
        }

        public void UpdateMaterial()
        {
            if (Structure.BlueprintTile != null)
            {
                Debug.Log("Structure Material Update triggered: " + Structure.BlueprintVisualState.ToString());
                switch (Structure.BlueprintVisualState)
                {
                    case Blueprint.VisualState.Preview: Geometry.SetAsPreview(); break;
                    case Blueprint.VisualState.PreviewOverlapping: Geometry.SetAsPreviewOverlapping(); break;
                    case Blueprint.VisualState.Valid: Geometry.SetAsBlueprint(); break;
                    case Blueprint.VisualState.Invalid: Geometry.SetAsBluePrintInvalid(); break;
                    case Blueprint.VisualState.Overlapping: Geometry.SetAsBluePrintOverlapping(); break;
                }
            }
            else
            {
                Geometry.SetAsBase();
            }
        }
    }
}
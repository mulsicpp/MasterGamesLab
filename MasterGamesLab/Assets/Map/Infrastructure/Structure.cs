
using Unity.Netcode;
using Networking;
using Map.Blueprint;
using Map.Fleet;
using UnityEngine;
using Map.GeometryGeneration;
using Map.Hoverables;

namespace Map.Infrastructure
{
    public abstract class Structure : Timestamped, IMapEntity
    {
        [System.Serializable]
        public enum StructureType : byte
        {
            Producer,
            Consumer,
            CarPark,
            Port,
            TrainStation
        }

        public interface IStructureState : IState
        {
            public StructureType Type { get; }
        }

        public struct CommonStructureState : IState, INetworkSerializeByMemcpy
        {
            public StructureIndex Index;
            public TileId TileId;

            public int ArrayIndex { get => Index; set => Index = new StructureIndex((byte)value); }
            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public abstract StructureType Type { get; }
        public readonly StructureIndex Index;

        public StructureId Id => new StructureId(Type, Index);
        public int IndexInStructures => Map.Instance.Infrastructure.StructureRanges[Type].Start.Value + Index;

        public EntityId EntityId => new(Map.Instance.EntityIdManager.StructureRange.Start.Value + IndexInStructures);

        public new Timestamp Timestamp => base.Timestamp;

        public virtual Player.Player Owner => null;

        public bool RendererUpdateTriggered;
        public bool RendererRebuildTriggered;

        public StructureRenderer Renderer { get; private set; }
        public abstract GameObject StructurePrefab { get; }

        private Tile tile;
        public Tile Tile
        {
            get => tile;
            set
            {
                if (tile == value) return;
                tile?.TriggerGeometryChange();
                if (tile != null)
                {
                    tile.Structure = null;
                }
                if (value != null)
                {
                    if (value.Structure != null)
                        value.Structure.Tile = null;
                    value.Structure = this;
                }
                tile = value;
                if (tile != null)
                {
                    tile.TriggerGeometryChange();
                    OnStructureSpawned();
                }
                base.Touch();
                TriggerRendererRebuild();
            }
        }

        private Tile blueprintTile;
        public Tile BlueprintTile
        {
            get => blueprintTile;
            set
            {
                if (blueprintTile == value) return;
                blueprintTile?.TriggerGeometryChange();
                if (blueprintTile != null)
                {
                    blueprintTile.BlueprintStructure = null;
                }
                if (value != null)
                {
                    if (value.BlueprintStructure != null)
                        value.BlueprintStructure.BlueprintTile = null;
                    value.BlueprintStructure = this;
                }
                blueprintTile = value;
                blueprintTile?.TriggerGeometryChange();
                TriggerRendererRebuild();
            }
        }

        private bool blueprintPreview;

        public bool BlueprintPreview
        {
            get => blueprintPreview;
            set
            {
                blueprintPreview = value;
                TriggerRendererUpdate();
            }
        }

        private bool blueprintIsValid;

        public bool BlueprintIsValid
        {
            get { return blueprintIsValid; }
            set
            {
                blueprintIsValid = value;
                TriggerRendererUpdate();
            }
        }

        public int BlueprintCost = 0;

        public VisualState BlueprintVisualState
        {
            get
            {
                if (BlueprintTile == null) return VisualState.Valid;
                if (BlueprintPreview) return BlueprintTile.Structure == null ? VisualState.Preview : VisualState.PreviewOverlapping;
                if (BlueprintTile.Structure?.Type == Type) return VisualState.Overlapping;
                return BlueprintIsValid ? VisualState.Valid : VisualState.Invalid;
            }
        }

        public bool Exists => tile != null;

        public CommonStructureState CommonState
        {
            get => new CommonStructureState { Index = Index, TileId = Tile?.Id ?? TileId.NONE };
            set => Tile = value.TileId != TileId.NONE && Map.Instance.Tiles[value.TileId] is Tile t ? t : null;
        }

        public static int GetMaxCountPerPlayer(StructureType type)
        {
            return type switch
            {
                StructureType.CarPark => Constants.MAX_GARAGES_PER_PLAYER,
                StructureType.Port => Constants.MAX_PORTS_PER_PLAYER,
                _ => -1
            };
        }

        protected Structure(StructureIndex index)
        {
            Index = index;
            Tile = null;
            Renderer = null;
            RendererRebuildTriggered = false;
            RendererUpdateTriggered = false;
            Touch();
        }

        public abstract ObjectWithFixedGeometry AttachStructureGeometry(Transform parent);

        public virtual void OnStructureSpawned() { }

        public virtual void Tick(float tickDuration) { }

        public override void Touch()
        {
            if (Tile != null || BlueprintTile != null)
                base.Touch();
        }

        public void TriggerRendererUpdate()
        {
            RendererUpdateTriggered = true;
        }

        public void TriggerRendererRebuild()
        {
            RendererRebuildTriggered = true;
            RendererUpdateTriggered = true;
        }

        public void RebuildRenderer()
        {
            if (Renderer != null)
            {
                Object.Destroy(Renderer.gameObject);
                Renderer = null;
            }
            if (Exists || BlueprintTile != null)
            {
                var gameObject = Object.Instantiate(StructurePrefab, Map.Instance.gameObject.transform);
                Renderer = gameObject.GetComponent<StructureRenderer>();
                Renderer.Init(this);
            }
            RendererRebuildTriggered = false;
            RendererUpdateTriggered = false;
        }

        public void ClearOutline()
        {
            Renderer?.Geometry.SetBaseLayer();
        }

        public void ShowOutline(Constants.OutlineData outlineData)
        {
            Renderer?.Geometry.SetOutlineLayer();
            Renderer?.Geometry.SetOutlineParameters(outlineData);
        }

        public void ShowHoverOutline(HoverState hoverState = HoverState.Valid)
        {
            var outlineData = hoverState switch
            {
                HoverState.Invalid => Constants.ROAD_BLUEPRINT_INVALID_OUTLINE,
                _ => Constants.HOVER_OUTLINE,
            };
            ShowOutline(outlineData);
        }
    }
}
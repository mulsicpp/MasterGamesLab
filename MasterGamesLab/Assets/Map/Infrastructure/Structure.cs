
using Unity.Netcode;
using Networking;
using Map.Blueprint;

namespace Map.Infrastructure
{
    public abstract class Structure : Timestamped
    {
        [System.Serializable]
        public enum StructureType : byte
        {
            Producer,
            Consumer,
            Garage,
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

        public new Timestamp Timestamp => base.Timestamp;

        public virtual Player.Player Owner => null;

        private Tile tile;
        public Tile Tile
        {
            get => tile;
            set
            {
                if (tile == value) return;
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
                    OnStructureSpawned();
                base.Touch();
            }
        }

        private Tile blueprintTile;
        public Tile BlueprintTile
        {
            get => blueprintTile;
            set
            {
                if (blueprintTile == value) return;
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
            }
        }

        private bool blueprintPreview;

        public bool BlueprintPreview
        {
            get => blueprintPreview;
            set
            {
                blueprintPreview = value;
                TriggerDirty();
            }
        }

        private bool blueprintIsValid;

        public bool BlueprintIsValid
        {
            get { return blueprintIsValid; }
            set
            {
                blueprintIsValid = value;
                TriggerDirty();
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
                StructureType.Garage => Constants.MAX_GARAGES_PER_PLAYER,
                StructureType.Port => Constants.MAX_PORTS_PER_PLAYER,
                _ => -1
            };
        }

        protected Structure(StructureIndex index)
        {
            Index = index;
            Tile = null;
            Touch();
        }

        public virtual void OnStructureSpawned() { }

        public virtual void Tick(float tickDuration) { }

        public override void Touch()
        {
            if (Tile != null)
                base.Touch();
        }

        public void TriggerDirty()
        {
            if (Tile != null) Tile.StructureDirty = true;
            if (BlueprintTile != null) BlueprintTile.StructureDirty = true;
        }
    }
}
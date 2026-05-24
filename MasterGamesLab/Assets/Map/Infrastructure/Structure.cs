
using Unity.Netcode;

namespace Map.Infrastructure
{
    public abstract class Structure
    {
        [System.Serializable]
        public enum StructureType : byte
        {
            Producer,
            Consumer,
            Garage,
            Port,
            TrainStation,
            None
        }

        public interface IStructureState: IState
        {
            public StructureType Type { get; }
        }

        public struct CommonStructureState : IState, INetworkSerializeByMemcpy
        {
            public StructureIndex Index;
            public TileId TileId;

            public int ArrayIndex { get => Index; set => Index = new StructureIndex((byte)value); }
        }

        public abstract StructureType Type { get; }

        public readonly StructureIndex Index;

        public Timestamp Timestamp { get; protected set; }

        private Tile tile;
        public Tile Tile
        {
            get { return tile; }
            set
            {
                if (tile != null)
                {
                    tile.Structure = null;
                }
                if (value != null)
                {
                    if (value.Structure != null)
                        value.Structure.tile = null;
                    value.Structure = this;
                }
                tile = value;
                Timestamp = Map.Instance.Timestamp;
            }
        }

        public bool Exists => tile != null;

        public CommonStructureState CommonState
        {
            get => new CommonStructureState { Index = Index, TileId = Tile?.Id ?? TileId.NONE };
            set => Tile = value.TileId != TileId.NONE && Map.Instance.Tiles[value.TileId] is Tile t ? t : null;
        }

        protected Structure(StructureIndex index)
        {
            Index = index;
            Tile = null;
            Timestamp = Map.Instance.Timestamp;
        }
    }
}
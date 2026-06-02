
using Unity.Netcode;
using Networking;
using static Map.Fleet.Vehicle;

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

        public interface IStructureState: IState
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

        public new Timestamp Timestamp => base.Timestamp;

        private Tile tile;
        public Tile Tile
        {
            get => tile;
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
                Touch();
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
    }
}

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

        public interface INetData {
            public StructureType Type { get; }
            public void SetIndex(StructureIndex index);
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

        protected Structure(StructureIndex index)
        {
            Index = index;
            Tile = null;
            Timestamp = Map.Instance.Timestamp;
        }
    }
}
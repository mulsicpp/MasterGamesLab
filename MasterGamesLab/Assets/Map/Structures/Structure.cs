
namespace Map.Structures
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

        public readonly StructureId Id;

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

        protected Structure(StructureId id, Tile tile = null)
        {
            Id = id;
            Tile = tile;
            Timestamp = Map.Instance.Timestamp;
        }
    }
}
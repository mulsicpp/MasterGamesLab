
namespace Map.Structures
{
    public abstract class Structure
    {
        public enum StructureType : byte
        {
            None,
            Producer,
            Consumer,
            Garage,
            Port,
            TrainStation
        }

        public readonly StructureId Id;
        public readonly StructureType Type;

        public Timestamp Timestamp { get; protected set; }

        private Tile tile;
        public Tile Tile { get { return tile; } set { tile = value; Timestamp = Map.Instance.Timestamp; } }

        protected Structure(StructureId id, StructureType type = StructureType.None, Tile tile = null)
        {
            Id = id;
            Type = type;
            this.tile = tile;
            Timestamp = Map.Instance.Timestamp;
        }
    }
}
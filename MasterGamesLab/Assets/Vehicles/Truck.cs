
namespace Vehicle
{
    public struct Truck
    {
        public struct TruckId
        {
            public byte PlayerID;
            public byte Index;
        }

        public enum TruckState : byte
        {
            NotCreated,
            Parked,
            Driving,
            OnFreighter
        };

        public TruckId Id { get; private set; }
        public TruckState State;
        public Resource Resource;

        public int TileId;

        public int[] Path;
        public float Progress;

        public byte FreighterIndex;
    }
}

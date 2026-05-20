
namespace Vehicle
{
    public struct Freighter
    {
        public struct FreighterId
        {
            public byte PlayerID;
            public byte Index;
        }

        public enum FreighterState : byte
        {
            Parked,
            Driving,
        };

        public FreighterId Id { get; private set; }
        public FreighterState State;

        public byte[] TruckIndices;

        public int TileId;

        public int[] Path;
        public float Progress;
    }
}
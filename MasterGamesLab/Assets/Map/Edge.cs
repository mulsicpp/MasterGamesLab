
namespace Map
{
    [System.Serializable]
    public struct Edge
    {
        public enum EdgeType : byte
        {
            Road,
            Canal,
            Rail
        }

        public int Id { get; private set; }

        public int StartTile { get; private set; }
        public int EndTile { get; private set; }

        public byte PlayerId { get; private set; }
        public EdgeType Type { get; private set; }

        public Edge(int id, int startTile, int endTile, byte playerId, EdgeType type)
        {
            Id = id;
            StartTile = startTile;
            EndTile = endTile;
            PlayerId = playerId;
            Type = type;
        }
    }
}
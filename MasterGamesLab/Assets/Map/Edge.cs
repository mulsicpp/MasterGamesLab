
namespace Map
{
    [System.Serializable]
    public struct Edge
    {
        public enum EdgeType : byte
        {
            None,
            Road,
            Canal,
            Rail
        }

        public int Id { get; private set; }

        public Tile StartTile { get; private set; }
        public Tile EndTile { get; private set; }

        public byte PlayerId;
        public EdgeType Type;

        public Edge(int id, Tile startTile, Tile endTile, byte playerId, EdgeType type)
        {
            Id = id;
            StartTile = startTile;
            EndTile = endTile;
            PlayerId = playerId;
            Type = type;
        }
    }
}
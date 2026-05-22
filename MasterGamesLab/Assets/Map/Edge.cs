
namespace Map
{
    public class Edge
    {
        public enum EdgeType : byte
        {
            None,
            Road,
            Canal,
            Rail
        }

        public EdgeId Id { get; private set; }

        public Tile StartTile { get; private set; }
        public Tile EndTile { get; private set; }

        public PlayerId PlayerId;
        public EdgeType Type;

        public Edge(EdgeId id, Tile startTile, Tile endTile, PlayerId playerId, EdgeType type)
        {
            Id = id;
            StartTile = startTile;
            EndTile = endTile;
            PlayerId = playerId;
            Type = type;
        }

        public bool CanBeRoad()
        {
            return StartTile.Type != Tile.TileType.Mountain && StartTile.Type != Tile.TileType.Water && EndTile.Type != Tile.TileType.Mountain && EndTile.Type != Tile.TileType.Water;
        }

        public bool CanBeCanal()
        {
            var startHasWater = StartTile.Type == Tile.TileType.Water || StartTile.CountEdgesWithType(EdgeType.Canal) > 0;
            var endHasWater = EndTile.Type == Tile.TileType.Water || EndTile.CountEdgesWithType(EdgeType.Canal) > 0;

            var startCanBuild = StartTile.Type == Tile.TileType.Plain || StartTile.Type == Tile.TileType.Forest;
            var endCanBuild = EndTile.Type == Tile.TileType.Plain || EndTile.Type == Tile.TileType.Forest;
            return (startHasWater && endCanBuild) || (startCanBuild && endHasWater);
        }

        public bool CanBeRail()
        {
            // TODO correct rail condition
            return false;
        }

        public bool CanBeType(EdgeType type)
        {
            switch(type)
            {
                case EdgeType.Road: return CanBeRail();
                case EdgeType.Canal: return CanBeCanal();
                case EdgeType.Rail: return CanBeRoad();
            }
            return true;
        }
    }
}
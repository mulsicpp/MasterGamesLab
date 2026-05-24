
using Unity.Netcode;

namespace Map
{
    public class Edge : INetObject<Edge.NetData>
    {
        [System.Serializable]
        public enum EdgeType : byte
        {
            None,
            Road,
            Canal,
            Rail
        }

        public readonly EdgeId Id;

        public readonly Tile StartTile;
        public readonly Tile EndTile;

        public Timestamp Timestamp { get; private set; }

        private EdgeType type;
        private PlayerId playerId;

        public EdgeType Type { get { return type; } set { type = value; Timestamp = Map.Instance.Timestamp; } }
        public PlayerId PlayerId { get { return playerId; } set { playerId = value; Timestamp = Map.Instance.Timestamp; } }


        public struct NetData : INetworkSerializeByMemcpy
        {
            public EdgeId Id;
            public EdgeType Type;
            public PlayerId PlayerId;
        }

        public Edge(EdgeId id, Tile startTile, Tile endTile, EdgeType type, PlayerId playerId)
        {
            Id = id;
            StartTile = startTile;
            EndTile = endTile;
            this.type = type;
            this.playerId = playerId;
            Timestamp = Map.Instance.Timestamp;
        }

        public bool CanBecomeRoad()
        {
            return Type == EdgeType.None && StartTile.Type != Tile.TileType.Mountain && StartTile.Type != Tile.TileType.Water && EndTile.Type != Tile.TileType.Mountain && EndTile.Type != Tile.TileType.Water;
        }

        public bool CanBecomeCanal()
        {
            if (Type != EdgeType.None) return false;
            var startHasWater = StartTile.Type == Tile.TileType.Water || StartTile.CountEdgesWithType(EdgeType.Canal) > 0;
            var endHasWater = EndTile.Type == Tile.TileType.Water || EndTile.CountEdgesWithType(EdgeType.Canal) > 0;

            var startCanBuild = StartTile.Type == Tile.TileType.Plain || StartTile.Type == Tile.TileType.Forest;
            var endCanBuild = EndTile.Type == Tile.TileType.Plain || EndTile.Type == Tile.TileType.Forest;
            return (startHasWater && endCanBuild) || (startCanBuild && endHasWater);
        }

        public bool CanBecomeRail()
        {
            // TODO correct rail condition
            return false;
        }

        public bool CanBecomeType(EdgeType type)
        {
            switch (type)
            {
                case EdgeType.Road: return CanBecomeRoad();
                case EdgeType.Canal: return CanBecomeCanal();
                case EdgeType.Rail: return CanBecomeRail();
            }
            return true;
        }

        public NetData GetNetData() => new NetData { Id = Id, Type = type, PlayerId = playerId };
    }
}
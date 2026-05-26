
using Unity.Netcode;

namespace Map
{
    public class Edge : Timestamped, ISynchableObject<Edge.EdgeState>
    {
        [System.Serializable]
        public enum EdgeType : byte
        {
            None,
            Road,
            Canal,
            Rail
        }

        public struct EdgeState : IState, INetworkSerializeByMemcpy
        {
            public EdgeId Id;
            public EdgeType Type;
            public PlayerId Owner;

            public int ArrayIndex { get => Id; set => Id = new EdgeId(value); }
            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public readonly EdgeId Id;

        public readonly Tile StartTile;
        public readonly Tile EndTile;

        public new Timestamp Timestamp => base.Timestamp;

        private EdgeType type;
        public EdgeType Type { get { return type; } set { type = value; Touch(); } }

        private PlayerId owner;
        public PlayerId Owner { get { return owner; } set { owner = value; Touch(); } }

        public EdgeState State { 
            get => new EdgeState { Id = Id, Type = type, Owner = owner };
            set { Type = value.Type; Owner = value.Owner; }
        }

        public Edge(EdgeId id, Tile startTile, Tile endTile, EdgeType type, PlayerId playerId)
        {
            Id = id;
            StartTile = startTile;
            EndTile = endTile;
            this.type = type;
            this.owner = playerId;
            Touch();
        }

        public void ApplyServerState(EdgeState state) { State = state; ResetDirty(); }

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
    }
}
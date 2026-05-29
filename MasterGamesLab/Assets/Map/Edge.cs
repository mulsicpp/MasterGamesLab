
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

        public struct EdgeMeshData
        {
            public EdgeId Id; 
            public EdgeType Type;
            public PlayerId Owner; // every player has a color; use PlayerManager.Instance.GetPlayerColor(PlayerId)
            public EdgeType BlueprintType; // the type of the edge in the blueprint
            public Blueprint.VisualState VisualState; // how should the blueprint edge be displayed
        }

        public readonly EdgeId Id;

        public readonly Tile StartTile;
        public readonly Tile EndTile;

        public new Timestamp Timestamp => base.Timestamp;

        private EdgeType type;
        public EdgeType Type { get { return type; } set { type = value; Touch(); meshChanged = true; } }

        private PlayerId owner;
        public PlayerId Owner { get { return owner; } set { owner = value; Touch(); meshChanged = true; } }

        private EdgeType blueprintType;
        public EdgeType BlueprintType { get { return blueprintType; } set { blueprintType = value; meshChanged = true; } }

        public Blueprint.VisualState VisualState
        {
            get
            {
                if (blueprintType == EdgeType.None) return Blueprint.VisualState.None;
                if (type == EdgeType.None) return Blueprint.VisualState.Valid;
                if (type == blueprintType) return Blueprint.VisualState.Overlapping;
                return Blueprint.VisualState.Invalid;
            }
        }

        private bool meshChanged;
        public bool MeshChanged => meshChanged;

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

        public void ApplyServerState(EdgeState state, double _) { State = state; ResetDirty(); }

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

        public EdgeMeshData RetrieveMeshData()
        {
            meshChanged = false;
            return new EdgeMeshData { 
                Id = Id,
                Type = Type,
                Owner = Owner,
                BlueprintType = BlueprintType,
                VisualState = VisualState,
            };
        }
    }
}
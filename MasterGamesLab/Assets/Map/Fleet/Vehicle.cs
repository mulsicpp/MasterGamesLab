using Unity.Netcode;

namespace Map.Fleet
{
    public abstract class Vehicle : Timestamped
    {
        [System.Serializable]
        public enum VehicleType : byte
        {
            Truck,
            Freighter,
            None
        }

        public interface IVehicleState : IState
        {
            public VehicleType Type { get; }
        }

        public struct CommonVehicleState : IState, INetworkSerializable
        {
            public VehicleIndex Index;
            public PlayerId Owner;
            public TileId[] RouteIds;
            public float RouteProgress;
            public TileId ParkedTileId;

            public int ArrayIndex { get => Index; set => Index = new VehicleIndex((byte)value); }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                TileId[] routeIds = null;
                serializer.SerializeValue(ref Index);
                serializer.SerializeValue(ref Owner);
                if (serializer.IsWriter)
                {
                    routeIds = RouteIds ?? new TileId[] { };
                    serializer.SerializeValue(ref routeIds);
                }
                else
                {
                    serializer.SerializeValue(ref routeIds);
                    RouteIds = routeIds.Length > 0 ? routeIds : null;
                }
                serializer.SerializeValue(ref RouteProgress);
                serializer.SerializeValue(ref ParkedTileId);
            }
        }

        public struct VehicleProgressState : INetworkSerializeByMemcpy
        {
            public VehicleType Type;
            public VehicleIndex Index;

            public float Progress;
        }

        public abstract VehicleType Type { get; }
        public readonly VehicleIndex Index;

        private PlayerId owner;
        public PlayerId Owner { get { return owner; } set { owner = value; Touch(); } }

        public bool Exists { get => Owner != PlayerId.NONE; }

        public new Timestamp Timestamp => base.Timestamp;

        private Tile[] route;
        public Tile[] Route { get { return route; } set { route = value; Touch(); } }
        public bool IsDriving => route != null;

        private bool progressDirty;
        public bool ProgressDirty => progressDirty;

        private float routeProgress;
        public float RouteProgress { get { return routeProgress; } set { routeProgress = value; PutTimestamp(); progressDirty = true; } }


        private Tile parkedTile;
        public Tile ParkedTile { get { return parkedTile; } set { parkedTile = value; Touch(); } }
        public bool IsParked => parkedTile != null;

        public CommonVehicleState CommonState
        {
            get
            {
                TileId[] routeIds = null;
                if (route != null)
                {
                    routeIds = new TileId[route.Length];
                    for (int i = 0; i < routeIds.Length; i++) routeIds[i] = route[i].Id;
                }

                return new CommonVehicleState
                {
                    Index = Index,
                    Owner = owner,
                    RouteIds = routeIds,
                    RouteProgress = routeProgress,
                    ParkedTileId = parkedTile?.Id ?? TileId.NONE
                };
            }

            set
            {
                Tile[] route = null;
                if (value.RouteIds != null)
                {
                    route = new Tile[value.RouteIds.Length];
                    for (int i = 0; i < route.Length; i++) route[i] = Map.Instance.Tiles[value.RouteIds[i]] is Tile r ? r : throw new System.NullReferenceException("Vehicle route cannot contain null tiles");
                }

                Owner = value.Owner;
                Route = route;
                RouteProgress = value.RouteProgress;
                ParkedTile = value.ParkedTileId != TileId.NONE && Map.Instance.Tiles[value.ParkedTileId] is Tile t ? t : null;
            }
        }

        public VehicleProgressState ProgressState => new VehicleProgressState { Type = Type, Index = Index, Progress = RouteProgress };

        protected Vehicle(VehicleIndex index)
        {
            Index = index;
            owner = PlayerId.NONE;
            route = null;
            routeProgress = 0;
            parkedTile = null;
            Touch();
        }

        public override void ResetDirty() { base.ResetDirty(); ResetProgressDirty(); }

        public void ResetProgressDirty()
        {
            progressDirty = false;
        }

        public virtual void Tick(float tickDuration)
        {
            if(!Exists) return;
            if (IsDriving)
            {
                if (Route.Length == 0) { Route = null; return; }
                if (Route.Length == 1 || RouteProgress < -0.01f) { ParkOn(Route[0]); return; }

                RouteProgress += tickDuration * Constants.TRUCK_SPEED_TPS;
                int lastIndex = Route.Length - 1;

                if (RouteProgress >= lastIndex) ParkOn(Route[lastIndex]);
            }
        }

        public virtual void ParkOn(Tile tile)
        {
            ParkedTile = tile; Route = null;
        }
    }
}
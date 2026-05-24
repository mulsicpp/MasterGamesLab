using Unity.Netcode;

namespace Map.Fleet
{
    public abstract class Vehicle
    {
        [System.Serializable]
        public enum VehicleType : byte
        {
            Truck,
            Freighter,
            None
        }

        public interface IVehicleState
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
                serializer.SerializeValue(ref Index);
                serializer.SerializeValue(ref Owner);
                serializer.SerializeValue(ref RouteIds);
                serializer.SerializeValue(ref RouteProgress);
                serializer.SerializeValue(ref ParkedTileId);
            }
        }

        public abstract VehicleType Type { get; }
        public readonly VehicleIndex Index;

        private PlayerId owner;
        public PlayerId Owner { get { return owner; } set { owner = value; Timestamp = Map.Instance.Timestamp; } }

        public bool Exists { get => Owner != PlayerId.NONE; }

        public Timestamp Timestamp { get; protected set; }

        private Tile[] route;
        public Tile[] Route { get { return route; } set { route = value; Timestamp = Map.Instance.Timestamp; } }
        public bool IsDriving => route != null;

        private float routeProgress;
        public float RouteProgress { get { return routeProgress; } set { routeProgress = value; Timestamp = Map.Instance.Timestamp; } }

        private Tile parkedTile;
        public Tile ParkedTile {  get { return parkedTile; } set { parkedTile = value; Timestamp = Map.Instance.Timestamp; } }
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

        protected Vehicle(VehicleIndex index)
        {
            Index = index;
            owner = PlayerId.NONE;
            route = null;
            routeProgress = 0;
            parkedTile = null;
            Timestamp = Map.Instance.Timestamp;
        }
    }
}
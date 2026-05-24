namespace Map.Vehicles
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
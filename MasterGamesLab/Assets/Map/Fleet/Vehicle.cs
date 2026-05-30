using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Networking;

namespace Map.Fleet
{
    public abstract class Vehicle : Timestamped, ISynchableObject<Vehicle.VehicleProgressState>
    {
        [System.Serializable]
        public enum VehicleType : byte
        {
            Truck,
            Freighter
        }

        public interface IVehicleState : IState
        {
            public VehicleType Type { get; }
            public CommonVehicleState CommonState { get; }
        }

        public struct CommonVehicleState : IState, INetworkSerializable
        {
            public VehicleIndex Index;
            public bool Exists;
            public TileId[] RouteIds;
            public float RouteProgress;
            public TileId ParkedTileId;

            public int ArrayIndex { get => Index; set => Index = new VehicleIndex((byte)value); }

            public int SerializedSize
            {
                get
                {
                    using (var writer = new FastBufferWriter(1300, Allocator.Temp))
                    {
                        writer.WriteNetworkSerializable(this);
                        return writer.Position;
                    }
                }
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                TileId[] routeIds = null;
                serializer.SerializeValue(ref Index);
                serializer.SerializeValue(ref Exists);
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

        public struct VehicleProgressState : IState, INetworkSerializeByMemcpy
        {
            public VehicleType Type;
            public VehicleIndex Index;

            public float Progress;

            public int ArrayIndex { get => GetOffsetFromType(Type) + Index; set => Type = GetTypeFromIndex(value, out Index); }
            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public abstract VehicleType Type { get; }
        public readonly VehicleIndex Index;

        public abstract PlayerId Owner { get; }

        private bool exists;
        public bool Exists { get { return exists; } set { exists = value; Touch(); } }

        public new Timestamp Timestamp => base.Timestamp;

        private Tile[] route;
        public Tile[] Route {
            get { return route; }
            set
            {
                if (value == null || value.Length < 2) route = null;
                else { route = value; parkedTile = null; smoothDriving.Reset(); }
                Touch();
            }
        }
        public bool IsDriving => route != null;

        private bool progressDirty;
        public bool ProgressDirty => progressDirty;

        private float routeProgress;
        public float RouteProgress { 
            get { return routeProgress; }
            set { 
                routeProgress = value;
                PutTimestamp();
                progressDirty = true;
            }
        }

        public abstract float SpeedTPS { get; }


        private Tile parkedTile;
        public Tile ParkedTile {
            get { return parkedTile; }
            set
            {
                parkedTile = value;
                if (value != null)
                {
                    route = null;
                    OnParked();
                }
                Touch();
            }
        }
        public bool IsParked => parkedTile != null;

        private SmoothDriving smoothDriving;

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
                    Exists = exists,
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

                Exists = value.Exists;
                Route = route;
                RouteProgress = value.RouteProgress;
                ParkedTile = value.ParkedTileId != TileId.NONE && Map.Instance.Tiles[value.ParkedTileId] is Tile t ? t : null;
            }
        }

        public VehicleProgressState ProgressState
        {
            get => new VehicleProgressState { Type = Type, Index = Index, Progress = RouteProgress };
            set { RouteProgress = value.Progress; }
        }

        VehicleProgressState ISynchableObject<VehicleProgressState>.State { get => ProgressState; set => ProgressState = value; }

        public void ApplyServerState(VehicleProgressState state, double serverTime) {

            ProgressState = state;
            ResetProgressDirty();
            smoothDriving.AddProgressUpdate(state, serverTime);
        }

        public static int GetOffsetFromType(VehicleType type)
        {
            int offset = 0;
            if (type == VehicleType.Truck) return offset;
            offset += Constants.MAX_TRUCK_COUNT;
            if (type == VehicleType.Freighter) return offset;
            offset += Constants.MAX_FREIGHTER_COUNT;
            return offset;
        }

        public static VehicleType GetTypeFromIndex(int index, out VehicleIndex localIndex)
        {
            if (index < Constants.MAX_TRUCK_COUNT)
            {
                localIndex = new((byte)index);
                return VehicleType.Truck;
            }
            else
            {
                localIndex = new((byte)(index - Constants.MAX_TRUCK_COUNT));
                return VehicleType.Freighter;
            }
        }

        public static int GetMaxCountPerPlayer(VehicleType type)
        {
            if (type == VehicleType.Truck) return Constants.MAX_TRUCKS_PER_PLAYER;
            else return Constants.MAX_FREIGHTERS_PER_PLAYER;
        }

        public static bool CanCross(ITile src, ITile dst, VehicleType type)
        {
            switch (type)
            {
                case VehicleType.Truck: return src?.FindEdgeTo(dst)?.Type == Edge.EdgeType.Road;
                case VehicleType.Freighter:
                    if (src?.Type == Tile.TileType.Water && dst?.Type == Tile.TileType.Water) return true;
                    else return src?.FindEdgeTo(dst)?.Type == Edge.EdgeType.Canal;
            }

            return false;
        }

        protected Vehicle(VehicleIndex index)
        {
            Index = index;
            exists = false;
            route = null;
            routeProgress = 0;
            parkedTile = null;
            smoothDriving = new SmoothDrivingPredictNewest(this);
            Touch();
        }

        public override void ResetDirty() { base.ResetDirty(); ResetProgressDirty(); }

        public void ResetProgressDirty()
        {
            progressDirty = false;
        }

        public virtual void Tick(float tickDuration)
        {
            if (!Exists) return;
            if (IsDriving)
            {
                if (Route.Length == 0) { Route = null; return; }
                if (Route.Length == 1 || RouteProgress < -0.01f) { ParkedTile = Route[0]; return; }

                RouteProgress += tickDuration * SpeedTPS;
                int lastIndex = Route.Length - 1;

                if (RouteProgress >= lastIndex) ParkedTile = Route[lastIndex];
            }
        }

        protected abstract void OnParked();

        public Vector3? PositionOnSphere
        {
            get
            {
                if (!Exists) return null;
                if (IsParked) return ParkedTile.PositionOnSphere;
                else if (IsDriving)
                {
                    float visualProgress = smoothDriving?.VisualProgress ?? RouteProgress;
                    if (visualProgress <= 0.0f) return Route[0].PositionOnSphere;
                    else if (visualProgress >= Route.Length - 1) return Route[Route.Length - 1].PositionOnSphere;
                    else
                    {
                        int index = (int)visualProgress;
                        float localProgress = visualProgress - index;

                        var pos1 = Route[index].PositionOnSphere;
                        var pos2 = Route[index + 1].PositionOnSphere;

                        return pos1 * (1.0f - localProgress) + pos2 * localProgress;
                    }
                }
                return null;
            }
        }
    }
}
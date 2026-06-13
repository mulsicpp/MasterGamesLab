using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Networking;
using System.Security.Cryptography;
using Map.Blueprint;
using Map.Hoverables;
using Map.OutlineEffect;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Map.GeometryGeneration;

namespace Map.Fleet
{
    public abstract class Vehicle : Timestamped, ISynchableObject<Vehicle.VehicleProgressState>, IHoverable, IOutlinable
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

            public int ArrayIndex
            {
                get => Index;
                set => Index = new VehicleIndex((byte)value);
            }

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

            public int ArrayIndex
            {
                get => GetOffsetFromType(Type) + Index;
                set { GetTypeFromIndex(value).Deconstruct(out Type, out Index); }
            }

            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public abstract VehicleType Type { get; }
        public readonly VehicleIndex Index;

        public VehicleId Id => new VehicleId(Type, Index);

        public abstract Player.Player Owner { get; }

        public abstract ObjectWithFixedGeometry AttachVehicleGeometry(Transform parent);

        private VehicleRenderer renderer;

        private bool exists;

        public bool Exists
        {
            get { return exists; }
            set
            {
                if (exists != value)
                {
                    exists = value;
                    Touch();
                }
            }
        }

        public new Timestamp Timestamp => base.Timestamp;

        private Tile[] route;

        public Tile[] Route
        {
            get { return route; }
            set
            {
                if (value == null || value.Length < 2) route = null;
                else
                {
                    route = value;
                    parkedTile = null;
                }

                Touch();
                smoothDriving?.Reset();
            }
        }

        public bool IsDriving => route != null;

        private bool progressDirty;
        public bool ProgressDirty => progressDirty;

        private float routeProgress;

        public float RouteProgress
        {
            get { return routeProgress; }
            set
            {
                routeProgress = value;
                PutTimestamp();
                progressDirty = true;
            }
        }

        public abstract float BaseSpeedTPS { get; }

        public float SpeedTPS => SpeedAt(RouteProgress);


        private Tile parkedTile;

        public Tile ParkedTile
        {
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


        private Tile blueprintTile;

        public Tile BlueprintTile
        {
            get { return blueprintTile; }
            set
            {
                if (blueprintTile != value)
                {
                    blueprintTile = value;
                    Touch();
                }
            }
        }

        public bool BlueprintPreview;
        public bool BlueprintValid;

        public bool BlueprintIsValid;
        public int BlueprintCost = 0;

        public VisualState BlueprintVisualState
        {
            get
            {
                if (BlueprintTile == null) return VisualState.Valid;
                if (BlueprintPreview) return VisualState.Preview;
                return BlueprintIsValid ? VisualState.Valid : VisualState.Invalid;
            }
        }

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
                    for (int i = 0; i < route.Length; i++)
                        route[i] = Map.Instance.Tiles[value.RouteIds[i]] is Tile r
                            ? r
                            : throw new System.NullReferenceException("Vehicle route cannot contain null tiles");
                }

                Exists = value.Exists;
                Route = route;
                RouteProgress = value.RouteProgress;
                ParkedTile = value.ParkedTileId != TileId.NONE && Map.Instance.Tiles[value.ParkedTileId] is Tile t
                    ? t
                    : null;
            }
        }

        public VehicleProgressState ProgressState
        {
            get => new VehicleProgressState { Type = Type, Index = Index, Progress = RouteProgress };
            set { RouteProgress = value.Progress; }
        }

        private double lastServerTime;

        VehicleProgressState ISynchableObject<VehicleProgressState>.State
        {
            get => ProgressState;
            set => ProgressState = value;
        }

        public static int GetOffsetFromType(VehicleType type)
        {
            int offset = 0;
            if (type == VehicleType.Truck) return offset;
            offset += Map.Instance.Fleet.Trucks.Count;
            if (type == VehicleType.Freighter) return offset;
            offset += Map.Instance.Fleet.Freighters.Count;
            return offset;
        }

        public static VehicleId GetTypeFromIndex(int index)
        {
            if (index < Map.Instance.Fleet.Trucks.Count)
                return new VehicleId(VehicleType.Truck, new((byte)index));
            else
                return new VehicleId(VehicleType.Freighter, new((byte)(index - Map.Instance.Fleet.Trucks.Count)));
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
            lastServerTime = 0;

            smoothDriving = new SmoothDrivingLinearSimulationInterpolation(this);
            Touch();
        }

        public void ApplyServerState(VehicleProgressState state, double serverTime)
        {
            if (serverTime > lastServerTime)
            {
                ProgressState = state;
                ResetProgressDirty();
                smoothDriving?.AddProgressUpdate(state, serverTime);
                lastServerTime = serverTime;
            }
        }

        public override void ResetDirty()
        {
            base.ResetDirty();
            ResetProgressDirty();
        }

        public void ResetProgressDirty()
        {
            progressDirty = false;
        }

        public virtual void Tick(float tickDuration)
        {
            if (!Exists) return;
            if (IsDriving)
            {
                if (Route.Length == 0)
                {
                    Route = null;
                    return;
                }

                if (Route.Length == 1 || RouteProgress < -0.01f)
                {
                    ParkedTile = Route[0];
                    return;
                }

                int lastIndex = Route.Length - 1;

                int oldTileIndex = Mathf.Clamp((int)(RouteProgress + 0.5f), 0, lastIndex);

                RouteProgress += tickDuration * SpeedTPS;

                int newTileIndex = Mathf.Clamp((int)(RouteProgress + 0.5f), 0, lastIndex);
                for (int i = oldTileIndex; i < newTileIndex; i++)
                {
                    var edge = Route[i].FindEdgeTo(Route[i + 1]);
                    if (edge == null) continue;

                    Owner?.TransferMoneyTo(edge.Owner, edge.GetTraversalCost(Owner));
                }

                if (RouteProgress >= lastIndex) ParkedTile = Route[lastIndex];
            }
        }

        public virtual void ClientTick(float tickDuration)
        {
            smoothDriving?.Tick(tickDuration);
        }

        protected abstract void OnParked();

        public float SpeedAt(float progress)
        {
            if (Route == null || Route?.Length < 2) return 0.0f;
            int index = Mathf.Clamp((int)progress, 0, Route.Length - 2);

            return BaseSpeedTPS * (Route[index].FindEdgeTo(Route[index + 1])?.GetSpeedMultiplier() ?? 1.0f);
        }

        public virtual VehicleTransform Transform
        {
            get
            {
                if (!Exists)
                {
                    return BlueprintTile?.ParkedVehicleTransform();
                }

                if (IsParked) return ParkedTile.ParkedVehicleTransform();
                else if (IsDriving)
                {
                    // float visualProgress = RouteProgress + SpeedTPS * (Time.time - Time.fixedTime);
                    float visualProgress = smoothDriving?.VisualProgress ?? RouteProgress;
                    if (visualProgress <= 0.0f) return Route[0].ParkedVehicleTransform();
                    else if (visualProgress >= Route.Length - 1)
                        return Route[Route.Length - 1].ParkedVehicleTransform();
                    else
                    {
                        int tileIndex = (int)(visualProgress + 0.5f);
                        float localProgress = visualProgress - tileIndex;

                        Vector3 position = default;
                        Vector3 tangent = default;

                        if (tileIndex == 0)
                        {
                            var curve = GeometryGeneration.ParametricCurve.FromTileToTileCenter(Route[tileIndex + 1],
                                Route[tileIndex]);
                            position = curve.Evaluate(1 - localProgress * 2f);
                            tangent = -curve.Derivative(1 - localProgress * 2f).normalized;
                        }
                        else if (tileIndex >= (Route.Length - 1))
                        {
                            var curve = GeometryGeneration.ParametricCurve.FromTileToTileCenter(Route[tileIndex - 1],
                                Route[tileIndex]);
                            position = curve.Evaluate(1 + localProgress * 2f);
                            tangent = curve.Derivative(1 + localProgress * 2f).normalized;
                        }
                        else
                        {
                            var curve = GeometryGeneration.ParametricCurve.FromTileToTileOverTile(Route[tileIndex - 1],
                                Route[tileIndex + 1], Route[tileIndex]);
                            position = curve.Evaluate(localProgress + 0.5f);
                            tangent = curve.Derivative(localProgress + 0.5f).normalized;
                        }

                        return new VehicleTransform
                        {
                            Position = position,
                            Up = position.normalized,
                            Forward = tangent.normalized,
                        }.AdjustUpVector();
                    }
                }

                return null;
            }
        }

        public void EvaluateGameobjectPresence()
        {
            if (Exists || BlueprintTile != null)
            {
                if (renderer == null)
                {
                    var gameObject = new GameObject("Vehicle");
                    gameObject.transform.parent = Map.Instance.gameObject.transform;
                    renderer = gameObject.AddComponent<VehicleRenderer>();
                    renderer.Init(this);
                    UpdateGameobject();
                }
            }
            else if (renderer != null)
            {
                Object.Destroy(renderer.gameObject);
                renderer = null;
            }
        }

        public virtual void UpdateGameobject()
        {
            EvaluateGameobjectPresence();

            if (renderer == null) return;
            var gameObject = renderer.gameObject;

            var t = Transform;
            if (t == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            var tProj = t; // Map.Instance.GetProjectedVehicleTransform(t);
            gameObject.transform.localPosition = tProj.Position;
            gameObject.transform.localRotation = Quaternion.LookRotation(tProj.Forward, tProj.Up);
        }

        public void ClearOutline()
        {
            renderer?.Geometry.SetBaseLayer();
        }

        public void ShowOutline(Constants.OutlineData outlineData)
        {
            renderer?.Geometry.SetOutlineLayer();
            renderer?.Geometry.SetOutlineParameters(outlineData);
        }

        public void ShowHoverOutline(HoverState hoverState = HoverState.Valid)
        {
            var outlineData = hoverState switch
            {
                HoverState.Invalid => Constants.ROAD_BLUEPRINT_INVALID_OUTLINE,
                _ => Constants.HOVER_OUTLINE,
            };
            ShowOutline(outlineData);
        }
    }
}
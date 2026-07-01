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
using System.Collections.Generic;
using static Unity.VectorGraphics.VectorUtils;
using System;
using System.Linq;

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
                get => Map.Instance.Fleet.VehicleRanges[Type].Start.Value + Index;
                set { VehicleIndexToId(value).Deconstruct(out Type, out Index); }
            }

            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public abstract VehicleType Type { get; }
        public readonly VehicleIndex Index;

        public VehicleId Id => new VehicleId(Type, Index);
        public int IndexInVehicles => Map.Instance.Fleet.VehicleRanges[Type].Start.Value + Index;

        public EntityId EntityId => new(Map.Instance.EntityIdManager.VehicleRange.Start.Value + IndexInVehicles);

        public abstract Player.Player Owner { get; }

        public VehicleRenderer Renderer { get; private set; }
        public abstract GameObject VehiclePrefab { get; }
        public Constants.OutlineData? Outline { get; private set; }

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

        public abstract bool IsIdle { get; }
        public LinkedList<VehicleAction> ActionQueue { get; private set; }
        public bool WaitingForActionResponse { get; private set; } = false;
        public event Action OnActionQueueChanged;

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

        public static VehicleId VehicleIndexToId(int index)
        {
            foreach (var (type, range) in Map.Instance.Fleet.VehicleRanges)
            {
                if (index >= range.Start.Value && index < range.End.Value)
                {
                    return new VehicleId(type, new((byte)(index - range.Start.Value)));
                }
            }

            return VehicleId.NONE;
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

        public bool CanDriveRoute(Player.Player player, TileId[] routeIds, out int publicCost,
            out Dictionary<Player.Player, int> enemyCosts)
        {
            publicCost = 0;
            enemyCosts = new();

            if (routeIds == null || routeIds.Length < 2) return false;
            Tile[] route = new Tile[routeIds.Length];

            for (int i = 0; i < routeIds.Length; i++)
            {
                if (routeIds[i] < 0 || routeIds[i] >= Map.Instance.Tiles.Count) return false;
                route[i] = Map.Instance.Tiles[routeIds[i]] as Tile;
            }

            return CanDriveRoute(player, route, out publicCost, out enemyCosts);
        }

        public bool CanDriveRoute(Player.Player player, Tile[] route, out int publicCost,
            out Dictionary<Player.Player, int> enemyCosts)
        {
            publicCost = 0;
            enemyCosts = new();

            if (!Exists || !IsParked || Owner != player) return false;
            if (route == null || route.Length < 2) return false;

            foreach (var p in Map.Instance.Players)
            {
                enemyCosts.Add(p, 0);
            }

            for (int i = 1; i < route.Length; i++)
            {
                if (!CanCross(route[i - 1], route[i], Type)) return false;

                if (route[i - 1]?.FindEdgeTo(route[i]) is Edge e)
                {
                    if (e.Owner == null)
                        publicCost += e.GetTraversalCost(player);
                    else
                        enemyCosts[e.Owner] += e.GetTraversalCost(player);
                }
            }

            int totalCost = publicCost;

            foreach (var (p, c) in enemyCosts)
            {
                totalCost += c;
            }

            return totalCost <= player.Cash;
        }

        protected Vehicle(VehicleIndex index)
        {
            Index = index;
            exists = false;
            route = null;
            routeProgress = 0;
            parkedTile = null;
            lastServerTime = 0;

            Renderer = null;

            ActionQueue = new();
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

        public abstract ObjectWithFixedGeometry AttachVehicleGeometry(Transform parent);

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

                // int oldTileIndex = Mathf.Clamp((int)(RouteProgress + 0.5f), 0, lastIndex);

                RouteProgress += tickDuration * SpeedTPS;

                // int newTileIndex = Mathf.Clamp((int)(RouteProgress + 0.5f), 0, lastIndex);
                // for (int i = oldTileIndex; i < newTileIndex; i++)
                // {
                //     var edge = Route[i].FindEdgeTo(Route[i + 1]);
                //     if (edge == null) continue;
                // 
                //     Owner?.TransferMoneyTo(edge.Owner, edge.GetTraversalCost(Owner));
                // }

                if (RouteProgress >= lastIndex) ParkedTile = Route[lastIndex];
            }
        }

        public abstract bool CanDoAction(VehicleAction action);
        public void SubmitAction(VehicleAction action)
        {
            WaitingForActionResponse = true;
            Map.Instance.RequestVehicleActionServerRpc(IndexInVehicles, action);
        }

        public void HandleActionResponse(bool success)
        {
            if (success && ActionQueue.Count > 0)
            {
                if (ActionQueue.First.Value.Type == VehicleAction.ActionType.LoadTruck && this is Truck truck)
                {
                    if (truck.Freighter?.ActionQueue.First?.Value.Type == VehicleAction.ActionType.WaitForTruck)
                    {
                        truck.Freighter.ActionQueue.RemoveFirst();
                        truck.Freighter.OnActionQueueChanged?.Invoke();
                    }
                }
                ActionQueue.RemoveFirst();
                OnActionQueueChanged?.Invoke();
            }
            WaitingForActionResponse = false;
        }

        protected abstract void OnParked();

        public virtual void ClientTick(float tickDuration)
        {
            if (!Exists) return;

            while (!WaitingForActionResponse && IsIdle && ActionQueue.Count > 0)
            {
                var nextAction = ActionQueue.First.Value;
                if (CanDoAction(nextAction))
                    SubmitAction(nextAction);
                else
                    break;
            }

            smoothDriving?.Tick(tickDuration);
        }


        public float SpeedAt(float progress)
        {
            if (Route == null || Route?.Length < 2) return 0.0f;
            int index = Mathf.Clamp((int)progress, 0, Route.Length - 2);

            return BaseSpeedTPS * (Route[index].FindEdgeTo(Route[index + 1])?.GetSpeedMultiplier() ?? 1.0f);
        }

        public virtual float? RemainingDriveTime
        {
            get
            {
                if (!IsDriving) return null;

                int lastIndex = Route.Length - 1;
                int previousTileIndex = Mathf.Clamp((int)RouteProgress, 0, lastIndex);

                if (previousTileIndex >= lastIndex) return 0;
                int nextTileIndex = previousTileIndex + 1;

                float remainingTime = (nextTileIndex - RouteProgress) / (BaseSpeedTPS *
                                                                         (Route[previousTileIndex]
                                                                             .FindEdgeTo(Route[nextTileIndex])
                                                                             ?.GetSpeedMultiplier() ?? 1.0f));
                for (int i = nextTileIndex; i < lastIndex; i++)
                {
                    remainingTime += 1.0f /
                                     (BaseSpeedTPS * (Route[i].FindEdgeTo(Route[i + 1])?.GetSpeedMultiplier() ?? 1.0f));
                }

                return remainingTime;
            }
        }

        public float VisualProgress => smoothDriving?.VisualProgress ?? RouteProgress;

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
                    float visualProgress = VisualProgress;
                    if (visualProgress <= 0.0f) return Route[0].ParkedVehicleTransform();
                    else if (visualProgress >= Route.Length - 1)
                        return Route[Route.Length - 1].ParkedVehicleTransform();
                    else
                    {
                        int tileIndex = (int)(visualProgress + 0.5f);
                        float localProgress = visualProgress - tileIndex;

                        Vector3 position;
                        Vector3 tangent;

                        ParametricCurve.CurveData curveType = Type switch
                        {
                            VehicleType.Freighter => ParametricCurve.CurveData.DefaultWaterCurve,
                            _ => ParametricCurve.CurveData.DefaultRoadCurve,
                        };

                        if (tileIndex == 0)
                        {
                            var curve = GeometryGeneration.ParametricCurve.FromTileToTileCenter(Route[tileIndex + 1],
                                Route[tileIndex], curveType);
                            position = curve.Evaluate(1 - localProgress * 2f);
                            tangent = -curve.Derivative(1 - localProgress * 2f).normalized;
                        }
                        else if (tileIndex >= (Route.Length - 1))
                        {
                            var curve = GeometryGeneration.ParametricCurve.FromTileToTileCenter(Route[tileIndex - 1],
                                Route[tileIndex], curveType);
                            position = curve.Evaluate(1 + localProgress * 2f);
                            tangent = curve.Derivative(1 + localProgress * 2f).normalized;
                        }
                        else
                        {
                            var curve = GeometryGeneration.ParametricCurve.FromTileToTileOverTile(Route[tileIndex - 1],
                                Route[tileIndex + 1], Route[tileIndex], curveType);
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

        public Tile GetTileLocationAfterAction(LinkedListNode<VehicleAction> actionNode, out bool loaded)
        {
            loaded = false;
            if (actionNode == null)
            {
                if (this is Truck t && t.Freighter != null)
                {
                    loaded = true;
                    return t.Freighter.GetTileLocationAfterAction(t.Freighter.ActionQueue.First, out _);
                }
                if (IsParked) return ParkedTile;
                if (IsDriving) return Route[^1];
                return null;
            } else
            {
                var action = actionNode.Value;
                if(action.Type == VehicleAction.ActionType.WaitForTruck)
                {
                    return GetTileLocationAfterAction(actionNode.Previous, out _);
                }
                else
                {
                    loaded = action.Type == VehicleAction.ActionType.LoadTruck;
                    return Map.Instance.Tiles[action.TargetTileId] as Tile;
                }
            }
        }

        public Tile GetTileLocationAfterAllActions(out bool loaded) => GetTileLocationAfterAction(ActionQueue.Last, out loaded);

        public void EvaluateGameObjectPresence()
        {
            if (Exists || BlueprintTile != null)
            {
                if (Renderer == null)
                {
                    var gameObject = UnityEngine.Object.Instantiate(VehiclePrefab, Map.Instance.gameObject.transform);
                    Renderer = gameObject.GetComponent<VehicleRenderer>();
                    Renderer.Init(this);
                }
            }
            else if (Renderer != null)
            {
                UnityEngine.Object.Destroy(Renderer.gameObject);
                Renderer = null;
            }
        }

        public void ClearOutline()
        {
            Outline = null;
            //Renderer?.Geometry.SetBaseLayer();
        }

        public void ShowOutline(Constants.OutlineData outlineData)
        {
            Outline = outlineData;
            //Renderer?.Geometry.SetOutlineLayer();
            //Renderer?.Geometry.SetOutlineParameters(outlineData);
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

        public void EnqueueAction(VehicleAction action)
        {
            if (ActionQueue.Count < Constants.MAX_VEHICLE_ACTION_COUNT_PER_VEHICLE) {
                ActionQueue.AddLast(action);
                OnActionQueueChanged?.Invoke();
            }
        }

        public void DeleteActionsAt(int index)
        {
            while (ActionQueue.Count > index)
            {
                ActionQueue.RemoveLast();
            }
            OnActionQueueChanged?.Invoke();
        }
    }
}
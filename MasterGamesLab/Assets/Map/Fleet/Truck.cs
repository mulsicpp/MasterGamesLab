using Map.GeometryGeneration;
using Map.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using Networking;
using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Map.Fleet
{
    public class Truck : Vehicle, ISynchableObject<Truck.TruckState>
    {
        public struct TruckState : IState, IVehicleState, INetworkSerializable
        {
            public CommonVehicleState Common;

            public Good Good;
            public VehicleIndex FreighterIndex;

            public int ArrayIndex
            {
                get => Common.ArrayIndex;
                set => Common.ArrayIndex = value;
            }

            public VehicleType Type => VehicleType.Truck;
            public CommonVehicleState CommonState => Common;

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
                serializer.SerializeValue(ref Common);
                serializer.SerializeValue(ref Good);
                serializer.SerializeValue(ref FreighterIndex);
            }
        }

        public override VehicleType Type => VehicleType.Truck;
        public override Player.Player Owner => Map.Instance.Players[(byte)(Index / Constants.MAX_TRUCKS_PER_PLAYER)];

        public override GameObject VehiclePrefab => Map.Instance.TruckPrefab;

        public override float BaseSpeedTPS => Constants.TRUCK_BASE_SPEED_TPS;

        public override bool IsIdle => Freighter != null ? Freighter.IsIdle : IsParked;

        private Good good;

        public Good Good
        {
            get => good;
            set
            {
                good = value;
                Touch();
            }
        }

        private Freighter freighter;

        public Freighter Freighter
        {
            get => freighter;
            set
            {
                if (freighter != null)
                {
                    freighter.Truck = null;
                }

                if (value != null)
                {
                    if (value.Truck != null)
                        value.Truck.Freighter = null;
                    value.Truck = this;
                    ParkedTile = null;
                    Route = null;
                }

                freighter = value;
                Touch();
            }
        }

        public TruckState State
        {
            get => new TruckState
            { Common = CommonState, Good = Good, FreighterIndex = Freighter?.Index ?? VehicleIndex.NONE };
            set
            {
                CommonState = value.Common;
                Good = value.Good;
                Freighter = value.FreighterIndex != VehicleIndex.NONE
                    ? Map.Instance.Fleet.Freighters[value.FreighterIndex]
                    : null;
            }
        }

        public Truck(VehicleIndex index) : base(index)
        {
            good = Good.None;
            freighter = null;
        }

        public void ApplyServerState(TruckState state, double _)
        {
            State = state;
            ResetDirty();
        }

        public override ObjectWithFixedGeometry AttachVehicleGeometry(Transform parent)
        {
            // var id = Map.Instance.GetTileAndEdgeCount() + IndexInVehicles;
            return GeometriesManager.Instance.GetGameObjectGeometry(GeometriesManager.GeometryType.Truck,
                EntityId.Value, parent, Owner);
        }

        protected override void OnParked()
        {
        }

        public override void Tick(float tickDuration)
        {
            if (!Exists) return;

            base.Tick(tickDuration);

            if (ParkedTile == null || ParkedTile.Structure == null) return;

            if (ParkedTile.Structure is Producer p && p.Good != Good.None) Good = p.Good;
            else if (ParkedTile.Structure is Consumer c)
            {
                if (c.Request.Good != Good.None && c.Request.Good == Good)
                {
                    c.FulfillRequest(this);
                }
            }
        }

        public override bool CanDoAction(VehicleAction action)
        {
            if (action.TargetTileId < 0 || action.TargetTileId >= Map.Instance.Tiles.Count) return false;
            var tile = Map.Instance.Tiles[action.TargetTileId] as Tile;

            switch (action.Type)
            {
                case VehicleAction.ActionType.DriveRoute: return CanDriveRoute(Player.Player.Self, action.RouteIds, out _, out _);
                case VehicleAction.ActionType.LoadTruck:
                    Predicate<Freighter> additionalCondition = f => f.ActionQueue.Count == 0 || f.ActionQueue.First.Value.Type == VehicleAction.ActionType.WaitForTruck;
                    return CanBeLoaded(Player.Player.Self, tile, out _, out _, additionalCondition);
                case VehicleAction.ActionType.UnloadTruck: return CanBeUnloaded(Player.Player.Self, tile, out _);
            }
            return false;
        }

        public override VehicleTransform Transform
        {
            get
            {
                if (Exists && Freighter != null)
                {
                    return null;
                }

                return base.Transform;
            }
        }

        public bool CanBeLoaded(Player.Player player, Tile tile, out Freighter freighter, out int cost, Predicate<Freighter> additionalCondition = null)
        {
            cost = 0;
            freighter = null;

            if (Owner != player) return false;
            if (!(ParkedTile?.Structure?.Type == Structure.StructureType.Port)) return false;
            if (tile == null || tile.Type != Tile.TileType.Water || !tile.Neighbors.Contains(ParkedTile)) return false;

            freighter = Map.Instance.Fleet.Freighters.FirstOrDefault(f => f.Owner == Owner && f.ParkedTile == tile && f.Truck == null && (additionalCondition?.Invoke(f) ?? true));
            if (freighter == null) return false;

            var portOwner = ParkedTile.Structure.Owner;
            if (portOwner != null && portOwner != player)
            {
                cost = Constants.TRUCK_LOADING_COST_ENEMY;
                return cost <= player.Cash;
            }

            return true;
        }

        public bool CanBeUnloaded(Player.Player player, Tile tile, out int cost)
        {
            cost = 0;
            
            return Freighter?.CanUnloadTruck(player, tile, out cost) ?? false;
        }

        public override bool DriveDestinationCondition(Tile destination) => destination.Structure != null;
    }
}
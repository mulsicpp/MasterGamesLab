using Map.GeometryGeneration;
using Map.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using Networking;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using System.Linq;

namespace Map.Fleet
{
    public class Freighter : Vehicle, ISynchableObject<Freighter.FreighterState>
    {
        public struct FreighterState : IState, IVehicleState, INetworkSerializable
        {
            public CommonVehicleState Common;

            public int ArrayIndex
            {
                get => Common.ArrayIndex;
                set => Common.ArrayIndex = value;
            }

            public VehicleType Type => VehicleType.Freighter;
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
            }
        }

        public override VehicleType Type => VehicleType.Freighter;

        public override Player.Player Owner =>
            Map.Instance.Players[(byte)(Index / Constants.MAX_FREIGHTERS_PER_PLAYER)];


        public override GameObject VehiclePrefab => Map.Instance.FreighterPrefab;

        public override float BaseSpeedTPS => Constants.FREIGHTER_BASE_SPEED_TPS;

        public Truck Truck;

        public FreighterState State
        {
            get => new FreighterState { Common = CommonState };
            set { CommonState = value.Common; }
        }

        public Freighter(VehicleIndex index) : base(index)
        {
        }

        public void ApplyServerState(FreighterState state, double _)
        {
            State = state;
            ResetDirty();
        }

        public override ObjectWithFixedGeometry AttachVehicleGeometry(Transform parent)
        {
            // var id = Map.Instance.GetTileAndEdgeCount() + IndexInVehicles;
            return GeometriesManager.Instance.GetGameObjectGeometry(GeometriesManager.GeometryType.Freighter, EntityId.Value, parent);
        }

        protected override void OnParked()
        {
            // TODO implement
        }

        public bool CanLoadTruck(Player.Player player, Truck truck, out int cost)
        {
            cost = 0;
            if (!(truck?.ParkedTile?.Structure?.Type == Structure.StructureType.Port)) return false;
            if (Truck != null || truck.Owner != player || Owner != player || (!ParkedTile?.Neighbors.Contains(truck?.ParkedTile) ?? true)) return false;

            var portOwner = truck.ParkedTile.Structure.Owner;
            if (portOwner != null && portOwner != player)
            {
                cost = Constants.TRUCK_LOADING_COST_ENEMY;
                return cost <= player.Cash;
            }
            return true;
        }

        public bool CanUnloadTruck(Player.Player player, Tile tile, out int cost)
        {
            cost = 0;
            if (!(tile?.Structure?.Type == Structure.StructureType.Port)) return false;
            if (Truck == null || Truck.Owner != player || Owner != player) return false;
            if (!ParkedTile?.Neighbors.Contains(tile) ?? true) return false;

            var portOwner = tile.Structure.Owner;
            if (portOwner != null && portOwner != player)
            {
                cost = Constants.TRUCK_UNLOADING_COST_ENEMY;
                return cost <= player.Cash;
            }
            return true;
        }
    }
}
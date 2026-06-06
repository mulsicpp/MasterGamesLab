using Map.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using Networking;
using UnityEngine;

namespace Map.Fleet
{
    public class Truck : Vehicle, ISynchableObject<Truck.TruckState>
    {
        public struct TruckState : IState, IVehicleState, INetworkSerializable
        {
            public CommonVehicleState Common;

            public Good Good;
            public VehicleIndex FreighterIndex;

            public int ArrayIndex { get => Common.ArrayIndex; set => Common.ArrayIndex = value; }
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
        public override PlayerId Owner => new PlayerId((byte)(Index / Constants.MAX_TRUCKS_PER_PLAYER));
        public override float SpeedTPS => Constants.TRUCK_BASE_SPEED_TPS;

        private Good good;
        public Good Good { get => good; set { good = value; Touch(); } }

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
            get => new TruckState { Common = CommonState, Good = Good, FreighterIndex = Freighter?.Index ?? VehicleIndex.NONE };
            set { 
                CommonState = value.Common;
                Good = value.Good;
                Freighter = value.FreighterIndex != VehicleIndex.NONE ? Map.Instance.Fleet.Freighters[value.FreighterIndex] : null;
            }
        }

        public Truck(VehicleIndex index) : base(index)
        {
            good = Good.None;
            freighter = null;
        }

        public void ApplyServerState(TruckState state, double _) { State = state; ResetDirty(); }

        protected override void OnParked()
        {
            if(ParkedTile.Structure == null) return;

            if(ParkedTile.Structure is Producer p) Good = p.Good;
            else if(ParkedTile.Structure is Consumer c)
            {
                if(c.RequestedGood != Good.None && c.RequestedGood == Good)
                {
                    c.RequestedGood = Good.None;
                    Good = Good.None;
                    Map.Instance.Players[Owner].Earn(10); // TODO calculate reward properly
                }
            }

        }

        public override Vector3? PositionOnSphere
        {
            get
            {
                if(Exists && Freighter != null)
                {
                    return Freighter.PositionOnSphere * 1.02f;
                }
                return base.PositionOnSphere;
            }
        }
    }
}
using Map.Infrastructure;
using Unity.Collections;
using Unity.Netcode;

namespace Map.Fleet
{
    public class Truck : Vehicle, ISynchableObject<Truck.TruckState>
    {
        public struct TruckState : IState, IVehicleState, INetworkSerializable
        {
            public CommonVehicleState Common;

            public Good Good;

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
            }
        }

        public override VehicleType Type => VehicleType.Truck;
        public override float SpeedTPS => Constants.TRUCK_SPEED_TPS;

        private Good good;
        public Good Good { get => good; set { good = value; Touch(); } }

        public TruckState State
        {
            get => new TruckState { Common = CommonState, Good = Good };
            set { CommonState = value.Common; Good = value.Good; }
        }

        public Truck(VehicleIndex index) : base(index)
        {
            good = Good.None;
        }

        public void ApplyServerState(TruckState state) { State = state; ResetDirty(); }

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
                    PlayerManager.Instance.Players[Owner].Money += 10; // TODO calculate reward properly
                }
            }

        }
    }
}
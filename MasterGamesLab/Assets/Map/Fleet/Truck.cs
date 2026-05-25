using Map.Infrastructure;
using Unity.Netcode;

namespace Map.Fleet
{
    public class Truck : Vehicle, ISynchableObject<Truck.TruckState>
    {
        public struct TruckState : IState, INetworkSerializable
        {
            public CommonVehicleState Common;

            public Good Good;

            public int ArrayIndex { get => Common.ArrayIndex; set => Common.ArrayIndex = value; }
            public VehicleType Type => VehicleType.Truck;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Common);
                serializer.SerializeValue(ref Good);
            }
        }

        public override VehicleType Type => VehicleType.Truck;

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
    }
}
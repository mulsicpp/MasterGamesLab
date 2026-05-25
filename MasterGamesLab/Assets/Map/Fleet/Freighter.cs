using Map.Infrastructure;
using Unity.Collections;
using Unity.Netcode;

namespace Map.Fleet
{
    public class Freighter : Vehicle, ISynchableObject<Freighter.FreighterState>
    {
        public struct FreighterState : IState, IVehicleState, INetworkSerializable
        {
            public CommonVehicleState Common;

            public int ArrayIndex { get => Common.ArrayIndex; set => Common.ArrayIndex = value; }
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
        public override float SpeedTPS => Constants.FREIGHTER_SPEED_TPS;

        public FreighterState State
        {
            get => new FreighterState { Common = CommonState };
            set { CommonState = value.Common; }
        }

        public Freighter(VehicleIndex index) : base(index)
        {

        }

        public void ApplyServerState(FreighterState state) { State = state; ResetDirty(); }

        protected override void OnParked()
        {
            // TODO implement
        }
    }
}
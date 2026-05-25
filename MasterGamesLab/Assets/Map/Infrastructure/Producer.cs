
using Unity.Netcode;
using static Map.Fleet.Truck;

namespace Map.Infrastructure
{
    public class Producer : Structure, ISynchableObject<Producer.ProducerState>
    {
        public struct ProducerState : IState, IStructureState, INetworkSerializeByMemcpy
        {
            public CommonStructureState Common;
            public Good Good;

            public StructureType Type => StructureType.Producer;

            public int ArrayIndex { get => Common.ArrayIndex; set => Common.ArrayIndex = value; }
            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public override StructureType Type => StructureType.Producer;

        private Good good;
        public Good Good { get { return good; } set { good = value; Touch(); } }

        public ProducerState State
        {
            get => new ProducerState { Common = CommonState, Good = Good };
            set { CommonState = value.Common; Good = value.Good; }
        }

        public Producer(StructureIndex index) : base(index)
        {
            good = Good.None;
        }

        public void ApplyServerState(ProducerState state) { State = state; ResetDirty(); }
    }
}
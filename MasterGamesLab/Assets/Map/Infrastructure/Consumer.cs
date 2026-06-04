
using Unity.Netcode;
using Networking;

namespace Map.Infrastructure
{
    public class Consumer : Structure, ISynchableObject<Consumer.ConsumerState>
    {
        public struct ConsumerState : IState, IStructureState, INetworkSerializeByMemcpy
        {
            public CommonStructureState Common;
            public Good RequestedGood;

            public StructureType Type => StructureType.Consumer;

            public int ArrayIndex { get => Common.ArrayIndex; set => Common.ArrayIndex = value; }
            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public override StructureType Type => StructureType.Consumer;

        private Good requestedGood;
        public Good RequestedGood { get { return requestedGood; } set { requestedGood = value; Touch(); TriggerDirty(); } }

        public ConsumerState State
        {
            get => new ConsumerState { Common = CommonState, RequestedGood = RequestedGood };
            set { CommonState = value.Common; RequestedGood = value.RequestedGood; }
        }

        public Consumer(StructureIndex index) : base(index)
        {
            requestedGood = Good.None;
        }

        public void ApplyServerState(ConsumerState state, double _) { State = state; ResetDirty(); }
    }
}
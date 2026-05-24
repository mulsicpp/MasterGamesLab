
using Unity.Netcode;

namespace Map.Infrastructure
{
    public class Consumer : Structure, ISynchableObject<Consumer.ConsumerState>
    {
        public struct ConsumerState : IState, IStructureState, INetworkSerializeByMemcpy
        {
            public CommonStructureState Common;
            public Good RequestedGood;

            public int ArrayIndex { get => Common.ArrayIndex; set => Common.ArrayIndex = value; }
            public StructureType Type => StructureType.Consumer;
        }

        public override StructureType Type => StructureType.Consumer;

        private Good requestedGood;
        public Good RequestedGood { get { return requestedGood; } set { requestedGood = value; Timestamp = Map.Instance.Timestamp; } }

        public ConsumerState State
        {
            get => new ConsumerState { Common = CommonState, RequestedGood = RequestedGood };
            set { CommonState = value.Common; RequestedGood = value.RequestedGood; }
        }

        public Consumer(StructureIndex index) : base(index)
        {
            requestedGood = Good.None;
        }
    }
}
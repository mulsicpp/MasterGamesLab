using Networking;
using Unity.Netcode;
using static Map.Infrastructure.Producer;

namespace Map.Infrastructure
{
    public class Port : Structure, ISynchableObject<Port.PortState>
    {
        public struct PortState : IState, IStructureState, INetworkSerializeByMemcpy
        {
            public CommonStructureState Common;

            public StructureType Type => StructureType.Port;

            public int ArrayIndex { get => Common.ArrayIndex; set => Common.ArrayIndex = value; }
            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public override StructureType Type => StructureType.Port;
        public override Player.Player Owner => Map.Instance.Players[(byte)(Index / Constants.MAX_PORTS_PER_PLAYER)];

        public PortState State
        {
            get => new PortState { Common = CommonState };
            set { CommonState = value.Common; }
        }

        public Port(StructureIndex index) : base(index)
        { }

        public void ApplyServerState(PortState state, double _) { State = state; ResetDirty(); }
    }
}
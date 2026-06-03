using Networking;
using Unity.Netcode;
using static Map.Infrastructure.Producer;

namespace Map.Infrastructure
{
    public class Garage : Structure, ISynchableObject<Garage.GarageState>
    {
        public struct GarageState : IState, IStructureState, INetworkSerializeByMemcpy
        {
            public CommonStructureState Common;

            public StructureType Type => StructureType.Garage;

            public int ArrayIndex { get => Common.ArrayIndex; set => Common.ArrayIndex = value; }
            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public override StructureType Type => StructureType.Garage;

        public GarageState State
        {
            get => new GarageState { Common = CommonState };
            set { CommonState = value.Common; }
        }

        public Garage(StructureIndex index) : base(index)
        { }

        public void ApplyServerState(GarageState state, double _) { State = state; ResetDirty(); }
    }
}
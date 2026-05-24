
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Netcode;
using static Map.Edge;
using static Map.Infrastructure.Structure;

namespace Map.Infrastructure
{
    public class Producer : Structure, ISynchableObject<Producer.ProducerState>
    {
        public struct ProducerState : IState, IStructureState, INetworkSerializeByMemcpy
        {
            public CommonStructureState Common;
            public Good Good;

            public int ArrayIndex { get => Common.ArrayIndex; set => Common.ArrayIndex = value; }
            public StructureType Type => StructureType.Producer;
        }

        public override StructureType Type => StructureType.Producer;

        private Good good;
        public Good Good { get { return good; } set { good = value; Timestamp = Map.Instance.Timestamp; } }

        public ProducerState State
        {
            get => new ProducerState { Common = CommonState, Good = Good };
            set { CommonState = value.Common; Good = value.Good; }
        }

        public Producer(StructureIndex index) : base(index)
        {
            good = Good.None;
        }
    }
}
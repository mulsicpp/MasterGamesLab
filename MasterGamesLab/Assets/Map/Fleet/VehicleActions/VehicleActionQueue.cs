using Networking;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

namespace Map.Fleet
{
    public class VehicleActionQueue : Timestamped, ISynchableObject<VehicleActionQueue.VehicleActionQueueState>
    {
        public struct VehicleActionQueueState : IState, INetworkSerializable
        {
            public int VehicleIndex;
            public VehicleAction[] QueuedActions;

            public int ArrayIndex { get => VehicleIndex; set => VehicleIndex = value; }

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
                serializer.SerializeValue(ref VehicleIndex);
                serializer.SerializeValue(ref QueuedActions);
            }
        }

        public readonly int VehicleIndex;

        private Queue<VehicleAction> queuedActions;
        public int Count => queuedActions.Count;

        public VehicleActionQueueState State {
            get => new VehicleActionQueueState { VehicleIndex = VehicleIndex, QueuedActions = queuedActions.ToArray() };
            set => ReplaceQueue(value.QueuedActions);
        }

        public VehicleActionQueue(int vehicleIndex)
        {
            VehicleIndex = vehicleIndex;
            queuedActions = new();
            Touch();
        }

        public void ApplyServerState(VehicleActionQueueState state, double _)
        {
            State = state;
            ResetDirty();
        }

        public void Enqueue(VehicleAction action)
        {
            queuedActions.Enqueue(action);
            Touch();
        }

        public VehicleAction Dequeue()
        {
            var ret = queuedActions.Dequeue();
            Touch();
            return ret;
        }

        public VehicleAction Peek() => queuedActions.Peek();

        public void ReplaceQueue(IEnumerable<VehicleAction> actions)
        {
            queuedActions = new Queue<VehicleAction>(actions);
            Touch();
        }
    }
}
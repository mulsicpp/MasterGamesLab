using Map;
using Networking;
using System.Collections.Generic;

namespace Networking
{
    public interface ISynchableObject<T> where T : struct, IState
    {
        public Timestamp Timestamp { get; }

        public T State { get; set; }

        public void ApplyServerState(T state, double serverTime);
    }
}
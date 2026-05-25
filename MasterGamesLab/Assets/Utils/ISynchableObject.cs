using Map;
using System.Collections.Generic;

public interface ISynchableObject<T> where T : struct, IState
{
    public Timestamp Timestamp { get; }

    public T State { get; set; }

    public void ApplyServerState(T state);
}
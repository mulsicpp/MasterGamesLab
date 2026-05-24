
using Map;

public interface INetObject<T> where T : struct
{
    public Timestamp Timestamp { get; }

    public T GetNetData();
}
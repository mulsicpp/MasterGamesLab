using Map;

public abstract class Timestamped
{
    private Timestamp timestamp;
    public Timestamp Timestamp => timestamp;

    private bool dirty;
    public bool Dirty => dirty;

    public void PutTimestamp() { timestamp = Map.Map.Instance.Timestamp; }
    public void Touch() { dirty = true; PutTimestamp(); }
    public virtual void ResetDirty() { dirty = false; }
}
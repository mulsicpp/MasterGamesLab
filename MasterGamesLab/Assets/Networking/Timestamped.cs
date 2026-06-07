using Map;

namespace Networking
{
    public abstract class Timestamped
    {
        private Timestamp timestamp;
        public Timestamp Timestamp => timestamp;

        private bool dirty;
        public bool Dirty => dirty;

        public void PutTimestamp() { timestamp = Map.Map.Instance.Timestamp; }
        public virtual void Touch() { dirty = true; PutTimestamp(); }
        public virtual void ResetDirty() { dirty = false; }

        public virtual bool DirtyCheckAndReset()
        { 
            if(dirty)
            {
                ResetDirty();
                return true;
            }
            return false;
        }

        public virtual bool ChangedSince(Timestamp timestamp) => this.timestamp > timestamp;
    }
}
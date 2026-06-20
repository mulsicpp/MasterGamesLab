using Map.OutlineEffect;
using System.Collections.Generic;

namespace Map
{
    public interface IMapEntity : IOutlinable
    {
        public EntityId EntityId { get; }
    }
}
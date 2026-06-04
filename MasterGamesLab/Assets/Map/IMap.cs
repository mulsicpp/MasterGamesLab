using System.Collections.Generic;
using Map.Hoverables;

namespace Map
{
    public interface IMap
    {
        public IReadOnlyList<ITile> Tiles { get; }
        public IReadOnlyList<ITile> ActiveTiles { get; }

        public float Radius { get; }

        public int Resolution { get; }
    }
}
using System.Collections.Generic;

namespace Map
{
    public interface IMap
    {
        public IReadOnlyList<Tile> Tiles { get; }
        public IReadOnlyList<Tile> ActiveTiles { get; }

        public float Radius { get; }

        public int Resolution { get; }

        public float HexSize { get; }

        public Tile GetCurrentlyHoveredTile();
    }
}
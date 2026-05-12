using System.Collections.Generic;
using GeometryGeneration;

namespace Map
{
    public interface IMap
    {
        public IReadOnlyList<Tile> Tiles { get; }

        public float Radius { get; }

        public int Resolution { get; }

        public float HexSize { get; }

        public Tile GetCurrentlyHoveredTile();

        public List<Tile> ActiveTiles { get; set; }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public interface ITile
    {
        public int Id { get; }

        public Vector3 PositionOnSphere { get; }

        public Tile.TileType Type { get; set; }

        public bool Active { get; set; }

        public IReadOnlyList<Tile> Neighbors { get; }
    }
}
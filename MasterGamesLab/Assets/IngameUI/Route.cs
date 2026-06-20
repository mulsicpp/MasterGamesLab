using Map;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class Route
    {
        private TileId[] tiles;
        public TileId[] Tiles => tiles;

        public readonly Color Color;

        public RouteRenderer Renderer { get; private set; }

        public Route(TileId[] tiles, Color color)
        {
            this.tiles = tiles;
            Color = color;

            var gameObject = Object.Instantiate(Map.Map.Instance.RoutePrefab);
            Renderer = gameObject.GetComponent<RouteRenderer>();
            Renderer.Init(this);
        }

        public void SetRoute(TileId[] tiles)
        {
            this.tiles = tiles;
        }

        public static bool AreSame(TileId[] r1, TileId[] r2)
        {
            if(r1?.Length != r2?.Length) return false;

            for (int i = 0; i < r1?.Length; i++)
            {
                if (r1[i] != r2[i]) return false;
            }
            return true;
        }
    }
}
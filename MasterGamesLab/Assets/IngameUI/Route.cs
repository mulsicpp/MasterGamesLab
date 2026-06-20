using Map.GeometryGeneration.Edges;
using UnityEngine;

namespace UI
{
    public class Route
    {
        private TileId[] tiles;
        public TileId[] Tiles => tiles;

        public readonly FullRoadGeometry.FullRoadType Type;

        public RouteRenderer Renderer { get; private set; }

        public Route(TileId[] tiles, FullRoadGeometry.FullRoadType type)
        {
            this.tiles = tiles;
            Type = type;

            var gameObject = Object.Instantiate(Map.Map.Instance.RoutePrefab);
            Renderer = gameObject.GetComponent<RouteRenderer>();
            Renderer.Init(this);
        }

        public void SetRoute(TileId[] tiles)
        {
            if (tiles != null)
                SetRoute(tiles, GetRouteMidpoint(tiles, 0, tiles.Length - 1));
            else
                SetRoute(null, Vector3.zero);
        }

        public void SetRoute(TileId[] tiles, Vector3 pinPosition)
        {
            this.tiles = tiles;
            Renderer.Pin.transform.position = pinPosition;
            if (Renderer.Geometry != null)
            {
                Object.Destroy(Renderer.Geometry.gameObject);
                Renderer.Geometry = null;
            }

            if (tiles != null)
            {
                Renderer.Geometry = EdgeGeometryFactory.GenerateFullRoad(tiles, Type);
            }
        }

        public static bool AreSame(TileId[] r1, TileId[] r2)
        {
            if (r1?.Length != r2?.Length) return false;

            for (int i = 0; i < r1?.Length; i++)
            {
                if (r1[i] != r2[i]) return false;
            }

            return true;
        }

        public static Vector3 GetRouteMidpoint(TileId[] route, int startIndex, int endIndex)
        {
            if (route == null || route.Length == 0) return Vector3.zero;

            if ((endIndex - startIndex) % 2 == 0)
            {
                int midIndex = startIndex + (endIndex - startIndex) / 2;
                midIndex = Mathf.Clamp(midIndex, 0, route.Length - 1);

                return Map.Map.Instance.Tiles[route[midIndex]].PositionOnSphere;
            }
            else
            {
                int midIndex1 = Mathf.Clamp(startIndex + (endIndex - startIndex) / 2, 0, route.Length - 1);
                int midIndex2 = Mathf.Clamp(midIndex1 + 1, 0, route.Length - 1);

                return (Map.Map.Instance.Tiles[route[midIndex1]].PositionOnSphere +
                        Map.Map.Instance.Tiles[route[midIndex2]].PositionOnSphere) * 0.5f;
            }
        }
    }
}
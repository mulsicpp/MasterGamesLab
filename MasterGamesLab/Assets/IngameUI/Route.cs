using Map;
using Map.Fleet;
using Map.GeometryGeneration.Edges;
using System.Linq;
using UnityEngine;

namespace UI
{
    public class Route
    {
        public enum RouteType
        {
            Cheapest = 0,
            Fastest = 1,
            Queued = 2,
            Current = 3,
        }

        private TileId[] tileids;
        public TileId[] TileIds => tileids;

        public readonly RouteType Type;

        public float Duration { get; private set; }
        public int Cost { get; private set; }

        public RouteRenderer Renderer { get; private set; }

        public Route(RouteType type)
        {
            this.tileids = null;
            Type = type;

            var gameObject = Object.Instantiate(Map.Map.Instance.RoutePrefab);
            Renderer = gameObject.GetComponent<RouteRenderer>();
            Renderer.Init(this);
        }

        public void SetRoute(Vehicle vehicle, TileId[] tileIds, int index = 0)
        {
            if (tileIds != null)
                SetRoute(vehicle, tileIds, GetRouteMidpoint(tileIds, 0, tileIds.Length - 1), index);
            else
                SetRoute(vehicle, null, Vector3.zero, index);
        }

        public void SetRoute(Vehicle vehicle, TileId[] tileIds, Vector3 pinPosition, int index = 0)
        {
            this.tileids = tileIds;
            EvaluateDurationAndCost(vehicle);

            Renderer.Pin.transform.position = pinPosition;
            if (Renderer.Geometry != null)
            {
                Object.Destroy(Renderer.Geometry.gameObject);
                Renderer.Geometry = null;
            }

            if (tileIds != null)
            {
                Renderer.Geometry = EdgeGeometryFactory.GenerateRoute(tileIds, Type, index);
                Renderer.Geometry.transform.SetParent(Renderer.transform);
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

        private void EvaluateDurationAndCost(Vehicle v)
        {
            if (v == null || tileids == null)
            {
                Duration = 0;
                Cost = 0;
                return;
            }
            var tiles = TileIds.Select(id => Map.Map.Instance.Tiles[id] as Tile).ToArray();

            float duration = 0;
            int cost = 0;
            for (int i = 0; i < tiles.Length - 1; i++)
            {
                var edge = tiles[i].FindEdgeTo(tiles[i + 1]);
                duration += 1.0f / (v.BaseSpeedTPS * (edge?.GetSpeedMultiplier() ?? 1.0f));
                cost += edge?.GetTraversalCost(v.Owner) ?? 0;
            }

            Duration = duration;
            Cost = cost;
        }
    }
}
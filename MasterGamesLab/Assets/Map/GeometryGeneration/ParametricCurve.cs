using Unity.AppUI.UI;
using UnityEngine;

namespace Map.GeometryGeneration
{
    public class ParametricCurve
    {
        public struct CurveData
        {
            public float StartHeight;
            public float EndHeight;

            public CurveData(float startHeight, float endHeight)
            {
                StartHeight = startHeight;
                EndHeight = endHeight;
            }

            public static CurveData DefaultRoadCurve =>
                new CurveData(TileGeometryFactory.LAND_HEIGHT, TileGeometryFactory.LAND_HEIGHT);

            public static CurveData DefaultWaterCurve =>
                new CurveData(TileGeometryFactory.WATER_HEIGHT, TileGeometryFactory.WATER_HEIGHT);
        }

        private Vector3 a;
        private Vector3 b;
        private Vector3 c;
        private Vector3 d;

        public static ParametricCurve FromTileToTileOverTile(Tile startTile, Tile endTile, Tile tile,
            CurveData? type = null)
        {
            Ray rStart = default, rEnd = default;
            bool foundStart = false, foundEnd = false;
            foreach (var n in tile.NeighborTiles)
            {
                if (n.Tile == startTile)
                {
                    var pos = (n.LeftVertex + n.RightVertex) / 2f;
                    rStart = new Ray(pos, (tile.PositionOnSphere - pos).normalized);
                    foundStart = true;
                }

                if (n.Tile == endTile)
                {
                    var pos = (n.LeftVertex + n.RightVertex) / 2f;
                    rEnd = new Ray(pos, (tile.PositionOnSphere - pos).normalized);
                    foundEnd = true;
                }
            }

            if (!foundStart || !foundEnd) return null;

            if (Vector3.Dot(rStart.direction, rEnd.origin - rStart.origin) < 0) rStart.direction = -rStart.direction;
            if (Vector3.Dot(rEnd.direction, rStart.origin - rEnd.origin) < 0) rEnd.direction = -rEnd.direction;

            return FromRaysWithType(rStart, rEnd, type ?? CurveData.DefaultRoadCurve);
        }

        public static ParametricCurve FromTileToTileCenter(Tile startTile, Tile endTile,
            CurveData? type = null)
        {
            Ray ray = default;
            var found = false;
            foreach (var n in endTile.NeighborTiles)
            {
                if (n.Tile == startTile)
                {
                    var pos = (n.LeftVertex + n.RightVertex) / 2f;
                    ray = new Ray(pos, (endTile.PositionOnSphere - pos).normalized);
                    found = true;
                }
            }

            if (!found) return null;

            if (Vector3.Dot(ray.direction, endTile.PositionOnSphere - ray.origin) < 0) ray.direction = -ray.direction;

            return FromRaysWithType(ray, new Ray(endTile.PositionOnSphere, -ray.direction),
                type ?? CurveData.DefaultRoadCurve, 0.5f);
        }

        public static ParametricCurve FromTileToParkingPosition(Tile startTile, Tile endTile, Ray parkingRay, CurveData? type = null)
        {
            Ray ray = default;
            var found = false;
            foreach (var n in endTile.NeighborTiles)
            {
                if (n.Tile == startTile)
                {
                    var pos = (n.LeftVertex + n.RightVertex) / 2f;
                    ray = new Ray(pos, (endTile.PositionOnSphere - pos).normalized);
                    found = true;
                }
            }

            if (!found) return null;

            if (Vector3.Dot(ray.direction, endTile.PositionOnSphere - ray.origin) < 0) ray.direction = -ray.direction;
            if (Vector3.Dot(ray.direction, ray.origin - parkingRay.origin) < 0) parkingRay.direction = -parkingRay.direction;

            return FromRaysWithType(ray, parkingRay, type ?? CurveData.DefaultRoadCurve, 0.5f);
        }

        public static ParametricCurve FromEdgeToEdge(Edge start, Edge end, Tile tile, CurveData? type = null)
        {
            var p0 = (start.VertexA + start.VertexB) / 2f;
            // var dir0 = Vector3.Cross(start.VertexA, start.VertexB).normalized;
            var dir0 = (tile.PositionOnSphere - p0).normalized;

            var p3 = (end.VertexA + end.VertexB) / 2f;
            //var dir3 = Vector3.Cross(end.VertexA, end.VertexB).normalized;
            var dir3 = (tile.PositionOnSphere - p3).normalized;

            if (Vector3.Dot(dir0, p3 - p0) < 0) dir0 = -dir0;
            if (Vector3.Dot(dir3, p0 - p3) < 0) dir3 = -dir3;

            return FromRaysWithType(new Ray(p0, dir0), new Ray(p3, dir3), type ?? CurveData.DefaultRoadCurve);
        }

        public static ParametricCurve FromEdgeToTileCenter(Edge edge, Tile tile, CurveData? type = null)
        {
            var p0 = (edge.VertexA + edge.VertexB) / 2f;
            // var dir0 = Vector3.Cross(edge.VertexA, edge.VertexB).normalized;
            var dir0 = (tile.PositionOnSphere - p0).normalized;

            var dir3 = -dir0;
            var p3 = tile.PositionOnSphere;

            if (Vector3.Dot(dir0, p3 - p0) < 0) dir0 = -dir0;
            if (Vector3.Dot(dir3, p0 - p3) < 0) dir3 = -dir3;

            return FromRaysWithType(new Ray(p0, dir0), new Ray(p3, dir3), type ?? CurveData.DefaultRoadCurve, 0.5f);
        }

        private static ParametricCurve FromRaysWithType(Ray startRay, Ray endRay, CurveData type,
            float handleDistanceScale = 1.0f)
        {
            var p0 = startRay.origin;
            var dir0 = startRay.direction;

            var p3 = endRay.origin;
            var dir3 = endRay.direction;

            p0 = p0.normalized * (type.StartHeight + Map.Instance.TEST_ROAD_HEIGHT);
            p3 = p3.normalized * (type.EndHeight + Map.Instance.TEST_ROAD_HEIGHT);

            var p1 = p0 + Map.Instance.TEST_ROAD_HANDLE_DISTANCE * Map.Instance.TileScale * handleDistanceScale * dir0;
            var p2 = p3 + Map.Instance.TEST_ROAD_HANDLE_DISTANCE * Map.Instance.TileScale * handleDistanceScale * dir3;

            return FromBezierPoints(p0, p1, p2, p3);
        }

        private static ParametricCurve FromBezierPoints(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var a = -p0 + 3 * p1 - 3 * p2 + p3;
            var b = 3 * p0 - 6 * p1 + 3 * p2;
            var c = -3 * p0 + 3 * p1;
            var d = p0;
            return new ParametricCurve(a, b, c, d);
        }

        private ParametricCurve(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public Vector3 Evaluate(float t) => ((a * t + b) * t + c) * t + d;

        public Vector3 Derivative(float t) => (3 * a * t + 2 * b) * t + c;
    }
}
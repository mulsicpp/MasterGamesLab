using UnityEngine;

namespace Map.GeometryGeneration
{
    public class ParametricCurve
    {
        private Vector3 a;
        private Vector3 b;
        private Vector3 c;
        private Vector3 d;

        public static ParametricCurve FromEdgeToEdge(Edge start, Edge end, Tile tile)
        {
            var p0 = (start.VertexA + start.VertexB) / 2f;
            var dir0 = Vector3.Cross(start.VertexA, start.VertexB).normalized;

            var p3 = (end.VertexA + end.VertexB) / 2f;
            var dir3 = Vector3.Cross(end.VertexA, end.VertexB).normalized;

            if (Vector3.Dot(dir0, p3 - p0) < 0) dir0 = -dir0;
            if (Vector3.Dot(dir3, p0 - p3) < 0) dir3 = -dir3;

            switch (tile.Type)
            {
                case Tile.TileType.Water:
                    p0 = p0.normalized * (TileGeometryFactory.WATER_HEIGHT + Map.Instance.TEST_ROAD_HEIGHT);
                    p3 = p3.normalized * (TileGeometryFactory.WATER_HEIGHT + Map.Instance.TEST_ROAD_HEIGHT);
                    break;
                case Tile.TileType.Plain:
                case Tile.TileType.Forest:
                    p0 = p0.normalized * (TileGeometryFactory.LAND_HEIGHT + Map.Instance.TEST_ROAD_HEIGHT);
                    p3 = p3.normalized * (TileGeometryFactory.LAND_HEIGHT + Map.Instance.TEST_ROAD_HEIGHT);
                    break;
                case Tile.TileType.Mountain:
                default:
                    break;
            }

            var p1 = p0 + Map.Instance.TEST_ROAD_HANDLE_DISTANCE * dir0;
            var p2 = p3 + Map.Instance.TEST_ROAD_HANDLE_DISTANCE * dir3;

            return FromBezierPoints(p0, p1, p2, p3);
        }

        public static ParametricCurve FromEdgeToTileCenter(Edge edge, Tile tile)
        {
            var p0 = (edge.VertexA + edge.VertexB) / 2f;
            var dir0 = Vector3.Cross(edge.VertexA, edge.VertexB).normalized;

            var dir3 = -dir0;
            var p3 = tile.PositionOnSphere;

            switch (tile.Type)
            {
                case Tile.TileType.Water:
                    p0 = p0.normalized * (TileGeometryFactory.WATER_HEIGHT + Map.Instance.TEST_ROAD_HEIGHT);
                    p3 = p3.normalized * (TileGeometryFactory.WATER_HEIGHT + Map.Instance.TEST_ROAD_HEIGHT);
                    break;
                case Tile.TileType.Plain:
                case Tile.TileType.Forest:
                    p0 = p0.normalized * (TileGeometryFactory.LAND_HEIGHT + Map.Instance.TEST_ROAD_HEIGHT);
                    p3 = p3.normalized * (TileGeometryFactory.LAND_HEIGHT + Map.Instance.TEST_ROAD_HEIGHT);
                    break;
                case Tile.TileType.Mountain:
                default:
                    break;
            }

            var p1 = p0 + Map.Instance.TEST_ROAD_HANDLE_DISTANCE / 2 * dir0;
            var p2 = p3 + Map.Instance.TEST_ROAD_HANDLE_DISTANCE / 2 * dir3;

            return FromBezierPoints(p0, p1, p2, p3);
        }

        public static ParametricCurve FromBezierPoints(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
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
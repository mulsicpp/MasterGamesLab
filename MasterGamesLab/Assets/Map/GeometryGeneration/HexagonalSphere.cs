using System.Collections.Generic;
using GeometryGeneration;
using UnityEngine;

namespace Map.GeometryGeneration
{
    public static class HexagonalSphere
    {
        public static List<Tile> GenerateHexagonalSphere(float radius, int subdivisionLevel)
        {
            var icoSphereVertices = GenerateIcoSphereGeometry(radius, subdivisionLevel);
            return GenerateTiles(icoSphereVertices, radius);
        }

        /*public void UpdateTiles(IProjection projection, float projectionFactor)
        {
            foreach (var tile in Tiles)
            {
                var pointOnSphere = tile.CenterOnSphere;
                var projectedPoint = projection.Project(pointOnSphere);
                tile.Center = Vector3.Lerp(pointOnSphere, projectedPoint, projectionFactor);
            }
        }*/

        private static List<Point> GenerateIcoSphereGeometry(float radius, int subdivisionLevel)
        {
            var icosahedronFaces = GenerateIcosahedron(radius);
            return SubdivideIcosahedron(icosahedronFaces, subdivisionLevel);
        }

        private static List<Triangle> GenerateIcosahedron(float radius)
        {
            var h = radius / Mathf.Sqrt(5f);
            var ringR = 2f * radius / Mathf.Sqrt(5f);

            var vertices = new List<Point>(12) { new(0f, radius, 0f) }; // north pole

            for (var i = 0; i < 5; i++) // upper ring
            {
                var angle = i * 2f * Mathf.PI / 5f - Mathf.PI / 5f;
                vertices.Add(new Point(ringR * Mathf.Cos(angle), h, ringR * Mathf.Sin(angle)));
            }

            for (var i = 0; i < 5; i++) // lower ring
            {
                var angle = i * 2f * Mathf.PI / 5f;
                vertices.Add(new Point(ringR * Mathf.Cos(angle), -h, ringR * Mathf.Sin(angle)));
            }

            vertices.Add(new Point(0f, -radius, 0f)); // south pole

            var faces = new List<Triangle>(20);
            for (var i = 0; i < 5; i++)
            {
                var currentUpper = 1 + i;
                var nextUpper = 1 + (i + 1) % 5;

                var currentLower = 6 + i;
                var nextLower = 6 + (i + 1) % 5;

                // Top cap 
                faces.Add(new Triangle(vertices[0], vertices[nextUpper], vertices[currentUpper]));
                // Middle ring (upward pointing triangles)
                faces.Add(new Triangle(vertices[currentUpper], vertices[nextUpper], vertices[currentLower]));
                // Middle ring (downward pointing triangles)
                faces.Add(new Triangle(vertices[currentLower], vertices[nextUpper], vertices[nextLower]));
                // Bottom cap
                faces.Add(new Triangle(vertices[11], vertices[currentLower], vertices[nextLower]));
            }

            return faces;
        }

        private static List<Point> SubdivideIcosahedron(List<Triangle> icosahedronFaces, int subdivisionLevel)
        {
            var vertices = new List<Point>();
            foreach (var tile in icosahedronFaces)
            {
                var points = tile.Points;

                List<Point> bottomSide;
                var leftSide = SubdivideLine(points[0], points[1], subdivisionLevel);
                var rightSide = SubdivideLine(points[0], points[2], subdivisionLevel);
                var topSide = new List<Point> { GetCachedPoint(points[0].Position) };

                for (var i = 1; i <= subdivisionLevel + 1; i++)
                {
                    bottomSide = topSide;
                    topSide = SubdivideLine(leftSide[i], rightSide[i], i - 1);

                    for (var j = 0; j < i; j++)
                    {
                        _ = new Triangle(bottomSide[j], topSide[j], topSide[j + 1]);
                        if (j == 0) continue;

                        _ = new Triangle(bottomSide[j - 1], bottomSide[j], topSide[j]);
                    }
                }
            }

            return vertices;

            List<Point> SubdivideLine(Point start, Point end, int amount)
            {
                var newPoints = new List<Point> { GetCachedPoint(start.Position) };

                for (var i = 1; i <= amount; i++)
                {
                    var factor = (float)i / (amount + 1);
                    var x = start.Position.x * (1 - factor) + end.Position.x * factor;
                    var y = start.Position.y * (1 - factor) + end.Position.y * factor;
                    var z = start.Position.z * (1 - factor) + end.Position.z * factor;

                    newPoints.Add(GetCachedPoint(new Vector3(x, y, z)));
                }

                newPoints.Add(GetCachedPoint(end.Position));
                return newPoints;
            }

            Point GetCachedPoint(Vector3 position)
            {
                foreach (var storedPoint in vertices)
                {
                    if (storedPoint.ApproximatelyEqual(position))
                    {
                        return storedPoint;
                    }
                }

                var point = new Point(position, vertices.Count);
                vertices.Add(point);
                return point;
            }
        }

        private static List<Tile> GenerateTiles(List<Point> icoSpherePoints, float radius)
        {
            var tiles = new List<Tile>(icoSpherePoints.Count);
            foreach (var vertex in icoSpherePoints)
            {
                tiles.Add(new Tile(vertex, radius));
            }

            return tiles;
        }
    }
}
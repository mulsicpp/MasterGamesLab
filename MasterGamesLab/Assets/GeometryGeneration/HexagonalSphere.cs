using System.Collections.Generic;
using GeometryGeneration.Projections;
using UnityEditor;
using UnityEngine;

namespace GeometryGeneration
{
    public class HexagonalSphere
    {
        private readonly float hexSize;
        private MapMesh mapMesh;
        private readonly int radius;
        private readonly int subdivisionLevel;

        public List<Tile> Tiles { get; private set; }

        public HexagonalSphere(int radius, int subdivisionLevel, float hexSize)
        {
            this.radius = radius;
            this.subdivisionLevel = subdivisionLevel;
            this.hexSize = hexSize;

            Tiles = new List<Tile>();
        }

        public void GenerateMesh(MeshFilter meshFilter, float projectionFactor)
        {
            projectionFactor = Mathf.Clamp(projectionFactor, 0.001f, 1);
            Tiles.Clear();
            var icoSphereVertices = GenerateIcoSphereGeometry();
            GenerateTiles(icoSphereVertices);

            IProjection projection = new BerghausStarProjection(new Vector3(0, 1, 0).normalized, radius);

            foreach (var tile in Tiles)
            {
                var pointOnSphere = tile.CenterOnSphere;
                var projectedPoint = projection.Project(pointOnSphere);
                tile.Center = Vector3.Lerp(pointOnSphere, projectedPoint, projectionFactor);
                // tile.Center = Point.ProjectToSphere(tile.Center, radius);
                // tile.Center = new Vector3(tile.Center.x, tile.Center.y, tile.Center.z * 0.5f);
            }

            GenerateMesh();

            var mesh = new Mesh
            {
                vertices = mapMesh.Vertices.ToArray(),
                triangles = mapMesh.Triangles.ToArray()
            };
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
        }

        private List<Point> GenerateIcoSphereGeometry()
        {
            var icosahedronFaces = GenerateIcosahedron();
            return SubdivideIcosahedron(icosahedronFaces);
        }

        private List<Triangle> GenerateIcosahedron()
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

        private List<Point> SubdivideIcosahedron(List<Triangle> icosahedronFaces)
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

        private void GenerateTiles(List<Point> icoSpherePoints)
        {
            var cur = 0;
            foreach (var vertex in icoSpherePoints)
            {
                /*if (cur > Map.MaxSpawn)
                {
                    return;
                }*/

                Tiles.Add(new Tile(vertex, radius));
                cur++;
            }
        }

        private void GenerateMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var vertIdx = 0;
            foreach (var tile in Tiles)
            {
                tile.BuildFaces(hexSize, radius * 0.5f);

                foreach (var face in tile.Faces)
                {
                    vertices.Add(face.Points[0].Position);
                    vertices.Add(face.Points[1].Position);
                    vertices.Add(face.Points[2].Position);
                    triangles.AddRange(new[] { vertIdx, vertIdx + 1, vertIdx + 2 });
                    vertIdx += 3;
                }
            }

            mapMesh = new MapMesh(vertices, triangles);
        }
    }
}
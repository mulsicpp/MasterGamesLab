using System.Collections.Generic;
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

        public void GenerateMesh(MeshFilter meshFilter)
        {
            Tiles.Clear();
            var icoSphereVertices = GenerateIcoSphereGeometry();
            GenerateTiles(icoSphereVertices);
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

        private List<Face> GenerateIcosahedron()
        {
            var r = radius;
            var h = r / Mathf.Sqrt(5f);
            var ringR = 2f * r / Mathf.Sqrt(5f);

            var vertices = new List<Point>(12) { new(0f, r, 0f) }; // north pole

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

            vertices.Add(new Point(0f, -r, 0f)); // south pole

            var faces = new List<Face>(20);
            for (var i = 0; i < 5; i++)
            {
                var currentUpper = 1 + i;
                var nextUpper = 1 + (i + 1) % 5;

                var currentLower = 6 + i;
                var nextLower = 6 + (i + 1) % 5;

                // Top cap 
                faces.Add(new Face(vertices[0], vertices[nextUpper], vertices[currentUpper]));
                // Middle ring (upward pointing triangles)
                faces.Add(new Face(vertices[currentUpper], vertices[nextUpper], vertices[currentLower]));
                // Middle ring (downward pointing triangles)
                faces.Add(new Face(vertices[currentLower], vertices[nextUpper], vertices[nextLower]));
                // Bottom cap
                faces.Add(new Face(vertices[11], vertices[currentLower], vertices[nextLower]));
            }

            return faces;
        }

        private List<Point> SubdivideIcosahedron(List<Face> icosahedronFaces)
        {
            var vertices = new List<Point>();
            foreach (var tile in icosahedronFaces)
            {
                var points = tile.Points;

                List<Point> bottomSide;
                var leftSide = SubdivideLine(points[0], points[1], subdivisionLevel);
                var rightSide = SubdivideLine(points[0], points[2], subdivisionLevel);
                var topSide = new List<Point> { points[0] };

                for (var i = 1; i <= subdivisionLevel + 1; i++)
                {
                    bottomSide = topSide;
                    topSide = SubdivideLine(leftSide[i], rightSide[i], i - 1);

                    for (var j = 0; j < i; j++)
                    {
                        _ = new Face(bottomSide[j], topSide[j], topSide[j + 1]);
                        if (j == 0) continue;

                        _ = new Face(bottomSide[j - 1], bottomSide[j], topSide[j]);
                    }
                }
            }

            return vertices;

            List<Point> SubdivideLine(Point start, Point end, int amount)
            {
                var newPoints = new List<Point> { GetCachedPoint(start) };

                for (var i = 1; i <= amount; i++)
                {
                    var factor = (float)i / (amount + 1);
                    var x = start.Position.x * (1 - factor) + end.Position.x * factor;
                    var y = start.Position.y * (1 - factor) + end.Position.y * factor;
                    var z = start.Position.z * (1 - factor) + end.Position.z * factor;

                    newPoints.Add(GetCachedPoint(new Point(x, y, z)));
                }

                newPoints.Add(GetCachedPoint(end));
                return newPoints;
            }

            Point GetCachedPoint(Point point)
            {
                foreach (var storedPoint in vertices)
                    if (storedPoint.ApproximatelyEqual(point))
                        return storedPoint;

                vertices.Add(point);
                point.ClearNeighbors();
                return point;
            }
        }

        private void GenerateTiles(List<Point> icoSpherePoints)
        {
            foreach (var vertex in icoSpherePoints) Tiles.Add(new Tile(vertex, radius, hexSize));
        }

        private void GenerateMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var vertIdx = 0;
            foreach (var tile in Tiles)
            foreach (var face in tile.Faces)
            {
                vertices.Add(face.Points[0].Position);
                vertices.Add(face.Points[1].Position);
                vertices.Add(face.Points[2].Position);
                triangles.AddRange(new[] { vertIdx, vertIdx + 1, vertIdx + 2 });
                vertIdx += 3;
            }

            mapMesh = new MapMesh(vertices, triangles);
        }
    }
}
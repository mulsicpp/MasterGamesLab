using System;
using System.Collections.Generic;
using Map.GeometryGeneration;
using UnityEngine;

namespace Map
{
    public class Tile : ITile
    {
        private const float FLOAT_COMPARISON_DELTA = 1e-5f;

        public enum TileType
        {
            Water,
            Plain,
            Forest,
            Mountain
        }

        // Point data
        public Vector3 Position;
        private readonly List<Triangle> neighborTriangles;

        // Tile data
        public TileId Id { get; private set; }
        public MapChunk Chunk;
        public IReadOnlyList<ITile> Neighbors => neighbors;
        public Vector3 PositionOnSphere { get; private set; }
        public readonly List<Triangle> Faces;

        public IReadOnlyList<Edge> Edges => edges;

        public TileType Type
        {
            get => tileType;
            set
            {
                tileType = value;
                Chunk.Dirty = true;
            }
        }

        public bool Active
        {
            get => active;
            set
            {
                active = value;
                if (active)
                {
                    Map.Instance.AddActiveTile(this);
                }
                else
                {
                    Map.Instance.RemoveActiveTile(this);
                }

                Chunk.Dirty = true;
            }
        }

        private readonly List<Tile> neighbors;
        private readonly List<Vector3> cornerPositions;
        private Vector3 cornersCenter;
        private TileType tileType;
        private bool active;

        private readonly float randomValue;

        private List<Edge> edges;

        public Tile(Vector3 position)
        {
            Position = position;
            neighborTriangles = new List<Triangle>();

            // Initialize tile data for later
            Id = TileId.NONE;
            Chunk = null;
            neighbors = new List<Tile>(6);
            cornerPositions = new List<Vector3>(6);
            Faces = new List<Triangle>(4);
            randomValue = UnityEngine.Random.Range(0f, 1f);

            edges = new List<Edge>();
        }

        // Point Functions
        public void AddNeighborTriangle(Triangle triangle)
        {
            neighborTriangles.Add(triangle);
        }

        public bool ApproximatelyEqual(Vector3 other)
        {
            return
                Mathf.Abs(other.x - Position.x) <= FLOAT_COMPARISON_DELTA &&
                Mathf.Abs(other.y - Position.y) <= FLOAT_COMPARISON_DELTA &&
                Mathf.Abs(other.z - Position.z) <= FLOAT_COMPARISON_DELTA;
        }

        public static Vector3 ProjectToSphere(Vector3 position, float radius)
        {
            return position.normalized * radius;
        }

        // Tile Functions
        public void InitializeTile(TileId id, float sphereRadius, MapChunk chunk)
        {
            Id = id;
            Chunk = chunk;
            PositionOnSphere = ProjectToSphere(Position, sphereRadius);

            if (neighborTriangles.Count == 0)
            {
                throw new Exception($"Tile {Id} at {Position} has no neighbours");
            }

            // Sort the neighbors so that they are in the correct order for the tile faces
            var normal = Position.normalized;
            var tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.magnitude < 0.001f)
            {
                tangent = Vector3.Cross(normal, Vector3.right);
            }

            tangent.Normalize();
            var bitangent = Vector3.Cross(normal, tangent);

            neighborTriangles.Sort((a, b) =>
            {
                var vA = a.Center - Position;
                var angleA = Mathf.Atan2(Vector3.Dot(vA, bitangent), Vector3.Dot(vA, tangent));
                var vB = b.Center - Position;
                var angleB = Mathf.Atan2(Vector3.Dot(vB, bitangent), Vector3.Dot(vB, tangent));
                return angleA.CompareTo(angleB);
            });

            cornerPositions.Clear();
            cornersCenter = Vector3.zero;
            foreach (var triangle in neighborTriangles)
            {
                var current = ProjectToSphere(triangle.Center, sphereRadius);
                cornerPositions.Add(current);
                cornersCenter += current;
            }

            cornersCenter /= cornerPositions.Count;
        }

        public void InitializeNeighbors()
        {
            neighbors.Clear();
            foreach (var neighbor in neighborTriangles)
            {
                foreach (var point in neighbor.Points)
                {
                    if (!neighbors.Contains(point) && point != this)
                    {
                        neighbors.Add(point);
                    }
                }
            }
        }

        public void ClearEdges()
        {
            edges.Clear();
        }

        public void InitializeEdges(List<Edge> edgeList)
        {
            foreach (Tile n in neighbors)
            {
                if (n.Id < Id) continue;
                if (n.Type == TileType.Water && Type == TileType.Water) continue;
                if (n.Type == TileType.Mountain || Type == TileType.Mountain) continue;

                Edge edge = new Edge(new EdgeId(edgeList.Count), this, n, byte.MaxValue, Edge.EdgeType.None);

                edges.Add(edge);
                n.edges.Add(edge);
                edgeList.Add(edge);
            }
        }

        private static readonly float TanPI3 = Mathf.Tan(Mathf.PI / 3);

        private static readonly Vector2[] HexagonCoordinates = new Vector2[]
        {
            new(-256, 0),
            new(-128, 128 * TanPI3),
            new(128, 128 * TanPI3),
            new(256, 0),
            new(128, -128 * TanPI3),
            new(-128, -128 * TanPI3),
        };

        private static readonly Vector2 WaterCenter = new(256, 128 * TanPI3);
        private static readonly Vector2 MountainCenter = new(256, 512 + 128 * TanPI3);
        private static readonly Vector2 PlainCenter = new(512 + 256, 128 * TanPI3);

        private static readonly Vector2 TextureSize = new(1024, 1024);
        private static readonly Vector2 InvTextureSize = new(1f / TextureSize.x, 1f / TextureSize.y);

        public void BuildFaces(List<Vector3> vertices, List<int> triangles, List<Vector4> tileData,
            List<Vector4> materialData)
        {
            var startIdx = vertices.Count;
            var tileDataVec = GetTileData();

            vertices.Add(cornersCenter);
            tileData.Add(tileDataVec);
            materialData.Add(new Vector4(MountainCenter.x, MountainCenter.y, 0, 0) * InvTextureSize);

            for (var i = 0; i < cornerPositions.Count; i++)
            {
                vertices.Add(cornerPositions[i]);
                tileData.Add(tileDataVec);
                materialData.Add(new Vector4(MountainCenter.x + HexagonCoordinates[i].x,
                    MountainCenter.y + HexagonCoordinates[i].y, 0, 0) * InvTextureSize);
            }

            for (var i = 0; i < cornerPositions.Count; i++)
            {
                var current = i + 1;
                var next = (i + 1) % cornerPositions.Count + 1;

                triangles.Add(startIdx + 0);
                triangles.Add(startIdx + current);
                triangles.Add(startIdx + next);
            }

            var geometryVertices = new List<Tile>(cornerPositions.Count);
            foreach (var point in cornerPositions)
            {
                geometryVertices.Add(new Tile(point));
            }

            Faces.Clear();

            for (var i = 0; i < geometryVertices.Count - 2; i++)
            {
                Faces.Add(new Triangle(geometryVertices[0], geometryVertices[i + 1], geometryVertices[i + 2]));
            }
        }

        public Vector4 GetTileData()
        {
            //return new Vector4(Id + Map.ID_OFFSET, randomValue, active ? 1 : 0, 0);
            return new Vector4(Id + Map.ID_OFFSET, (float)Type, active ? 1 : 0, randomValue);
        }
    }
}
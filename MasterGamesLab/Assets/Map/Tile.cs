using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        public struct NeighborTile
        {
            public Tile Tile;
            public Vector3 LeftVertex;
            public Vector3 RightVertex;
        }

        // Point data
        public Vector3 Position;
        private readonly List<Triangle> neighborTriangles;

        // Tile data
        public int Id { get; private set; }
        public MapChunk Chunk;
        public IReadOnlyList<ITile> Neighbors => neighbors;
        public Vector3 PositionOnSphere { get; private set; }

        public IReadOnlyList<Edge> Edges => edges;

        public TileType Type
        {
            get => tileType;
            set
            {
                tileType = value;
                Chunk.GeometryChanged = true;
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

        public readonly float RandomValue;
        public readonly List<NeighborTile> NeighborTiles;
        private readonly List<Tile> neighbors;
        private TileType tileType;
        private bool active;
        private List<Edge> edges;
        private MapChunk.TileGeometryInformation tileGeometryInformation;

        public Tile(Vector3 position)
        {
            Position = position;
            neighborTriangles = new List<Triangle>();

            // Initialize tile data for later
            Id = -1;
            Chunk = null;
            neighbors = new List<Tile>(6);
            NeighborTiles = new List<NeighborTile>(6);
            RandomValue = UnityEngine.Random.Range(0f, 1f);
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
        public void InitializeTile(int id, float sphereRadius, MapChunk chunk)
        {
            Id = id;
            Chunk = chunk;
            PositionOnSphere = ProjectToSphere(Position, sphereRadius);
            tileGeometryInformation = new MapChunk.TileGeometryInformation
            {
                NumVertices = -1,
            };

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

            neighborTriangles.Sort((a, b) => Comparison(a.Center, b.Center));

            neighbors.Clear();
            NeighborTiles.Clear();

            for (var i = 0; i < neighborTriangles.Count; i++)
            {
                var t1 = neighborTriangles[i];
                var t2 = neighborTriangles[(i + 1) % neighborTriangles.Count];

                Tile commonTile = null;
                foreach (var p1 in t1.Points)
                {
                    if (p1 == this) continue;
                    foreach (var p2 in t2.Points)
                    {
                        if (p1 == p2)
                        {
                            commonTile = p1;
                            break;
                        }
                    }

                    if (commonTile != null) break;
                }

                if (commonTile != null)
                {
                    neighbors.Add(commonTile);
                    NeighborTiles.Add(new NeighborTile
                    {
                        Tile = commonTile,
                        LeftVertex = ProjectToSphere(t1.Center, sphereRadius),
                        RightVertex = ProjectToSphere(t2.Center, sphereRadius)
                    });
                }
            }

            return;

            int Comparison(Vector3 a, Vector3 b)
            {
                var vA = a - Position;
                var angleA = Mathf.Atan2(Vector3.Dot(vA, bitangent), Vector3.Dot(vA, tangent));
                var vB = b - Position;
                var angleB = Mathf.Atan2(Vector3.Dot(vB, bitangent), Vector3.Dot(vB, tangent));
                return angleA.CompareTo(angleB);
            }
        }

        /*public void InitializeNeighbors()
        {
            neighbors.Clear();
            neighborTiles.Clear();

            foreach (var triangle in neighborTriangles)
            {
                foreach (var point in triangle.Points)
                {
                    if (!neighbors.Contains(point) && point != this)
                    {
                        neighbors.Add(point);

                        neighborTiles.Add(new NeighborTile
                        {
                            Tile = point,
                            LeftVertex = ProjectToSphere(triangle.Center, sphereRadius),
                        })
                    }
                    else if (neighbors.Contains(point))
                    {
                    }
                }
            }
        }*/

        public void InitializeEdges(List<Edge> edgeList)
        {
            edges = new List<Edge>();
            foreach (var n in neighbors)
            {
                if (n.Id < Id) continue;
                if (n.Type == TileType.Water && Type == TileType.Water) continue;
                if (n.Type == TileType.Mountain || Type == TileType.Mountain) continue;

                var edge = new Edge(edgeList.Count, this, n, byte.MaxValue, Edge.EdgeType.None);

                edges.Add(edge);
                n.edges.Add(edge);
                edgeList.Add(edge);
            }
        }

        public void BuildFaces(MapChunk.ChunkGeometry cg)
        {
            tileGeometryInformation = TileGeometryFactory.BuildFaces(this, cg);
        }

        public void FillTileData(List<Vector4> tileDataList, List<Map.TreeData> treeDataList)
        {
            var tileData = GetTileData();
            for (var i = 0; i < tileGeometryInformation.NumVertices; i++)
            {
                tileDataList.Add(tileData);
            }

            for (var i = tileGeometryInformation.StartTreeIdx; i < tileGeometryInformation.EndTreeIdx; i++)
            {
                var tree = treeDataList[i];
                tree.Active = active ? 1 : 0;
                treeDataList[i] = tree;
            }
        }

        public Vector4 GetTileData()
        {
            //return new Vector4(Id + Map.ID_OFFSET, randomValue, active ? 1 : 0, 0);
            return new Vector4(Id + Map.ID_OFFSET, (float)Type, active ? 1 : 0, RandomValue);
        }
    }
}
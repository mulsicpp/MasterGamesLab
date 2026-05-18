using System;
using System.Collections.Generic;
using Map.GeometryGeneration;
using UnityEngine;

namespace Map
{
    public class Tile
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
        public int Id { get; private set; }
        public MapChunk Chunk;
        public readonly List<Tile> Neighbors;
        public readonly List<Triangle> Faces;

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

        private readonly List<Vector3> cornerPositions;
        private TileType tileType;
        private bool active;

        private readonly float randomValue;

        public Tile(Vector3 position)
        {
            Position = position;
            neighborTriangles = new List<Triangle>();

            // Initialize tile data for later
            Id = -1;
            Chunk = null;
            Neighbors = new List<Tile>(6);
            cornerPositions = new List<Vector3>(6);
            Faces = new List<Triangle>(4);
            randomValue = UnityEngine.Random.Range(0f, 1f);
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
            foreach (var triangle in neighborTriangles)
            {
                cornerPositions.Add(ProjectToSphere(triangle.Center, sphereRadius));
            }
        }

        public void InitializeNeighbors()
        {
            Neighbors.Clear();
            foreach (var neighbor in neighborTriangles)
            {
                foreach (var point in neighbor.Points)
                {
                    if (!Neighbors.Contains(point) && point != this)
                    {
                        Neighbors.Add(point);
                    }
                }
            }
        }

        public void BuildFaces(float tileSize = 1)
        {
            tileSize = Math.Clamp(tileSize, 0.001f, 1);

            var geometryVertices = new List<Tile>(cornerPositions.Count);
            foreach (var point in cornerPositions)
            {
                geometryVertices.Add(new Tile(Vector3.Lerp(Position, point, tileSize)));
            }

            Faces.Clear();
            for (var i = 0; i < geometryVertices.Count - 2; i++)
            {
                Faces.Add(new Triangle(geometryVertices[0], geometryVertices[i + 1], geometryVertices[i + 2]));
            }
        }

        public Vector4 GetTileData()
        {
            return new Vector4(Id + Map.ID_OFFSET, randomValue, active ? 1 : 0, 0);
        }
    }
}
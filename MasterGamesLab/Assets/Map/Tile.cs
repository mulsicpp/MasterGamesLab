using System;
using System.Collections.Generic;
using Map.GeometryGeneration;
using UnityEngine;

namespace Map
{
    public class Tile
    {
        public enum TileType
        {
            Water,
            Plain,
            Forest,
            Mountain
        }

        public readonly int Id;
        public readonly MapChunk Chunk;
        public List<Triangle> Faces;

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

        public Vector3 Center
        {
            get => center.Position;
            set => center.Position = value;
        }

        private readonly List<Vector3> cornerPositions;
        private TileType tileType;
        private bool active;

        private readonly Point center;
        private readonly float randomValue;

        public Tile(Point center, float sphereRadius, MapChunk chunk)
        {
            Id = center.Id;
            Chunk = chunk;
            randomValue = UnityEngine.Random.Range(0f, 1f);
            this.center = center;

            if (center.Neighbors.Count == 0)
            {
                throw new Exception($"Tile {Id} at {center.Position} has no neighbours");
            }

            // Sort the neighbors so that they are in the correct order for the tile faces
            var normal = center.Position.normalized;
            var tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.magnitude < 0.001f)
            {
                tangent = Vector3.Cross(normal, Vector3.right);
            }

            tangent.Normalize();
            var bitangent = Vector3.Cross(normal, tangent);

            center.Neighbors.Sort((a, b) =>
            {
                var vA = a.Center - center.Position;
                var angleA = Mathf.Atan2(Vector3.Dot(vA, bitangent), Vector3.Dot(vA, tangent));
                var vB = b.Center - center.Position;
                var angleB = Mathf.Atan2(Vector3.Dot(vB, bitangent), Vector3.Dot(vB, tangent));
                return angleA.CompareTo(angleB);
            });

            cornerPositions = new List<Vector3>(center.Neighbors.Count);
            foreach (var triangle in center.Neighbors)
            {
                cornerPositions.Add(Point.ProjectToSphere(triangle.Center, sphereRadius));
            }
        }

        public void BuildFaces(float tileSize = 1)
        {
            tileSize = Math.Clamp(tileSize, 0.001f, 1);

            var geometryVertices = new List<Point>(cornerPositions.Count);
            foreach (var point in cornerPositions)
            {
                geometryVertices.Add(new Point(Vector3.Lerp(Center, point, tileSize)));
            }

            Faces = new List<Triangle>(4);
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
using System.Collections.Generic;
using UnityEngine;

namespace Map.GeometryGeneration
{
    public class Point
    {
        private const float FLOAT_COMPARISON_DELTA = 1e-5f;

        public int Id { get; private set; }
        public Vector3 Position;
        public readonly List<Triangle> Neighbors;

        public Point(Vector3 position, int id = -1)
        {
            Id = id;
            Position = position;
            Neighbors = new List<Triangle>();
        }

        public Point(float x, float y, float z, int id = -1)
        {
            Id = id;
            Position = new Vector3(x, y, z);
            Neighbors = new List<Triangle>();
        }

        public void SetId(int id) => Id = id;

        public void AddNeighbor(Triangle neighbor)
        {
            Neighbors.Add(neighbor);
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
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace GeometryGeneration
{
    public class Point
    {
        private const float FLOAT_COMPARISON_DELTA = 1e-5f;

        public readonly List<Face> Neighbors;
        public Vector3 Position;

        public Point(Vector3 position)
        {
            Position = position;
            Neighbors = new List<Face>();
        }

        public Point(float x, float y, float z)
        {
            Position = new Vector3(x, y, z);
            Neighbors = new List<Face>();
        }

        public void ClearNeighbors()
        {
            Neighbors.Clear();
        }

        public void AddNeighbor(Face neighbor)
        {
            Neighbors.Add(neighbor);
        }

        public bool ApproximatelyEqual(Point other)
        {
            return
                Mathf.Abs(other.Position.x - Position.x) <= FLOAT_COMPARISON_DELTA &&
                Mathf.Abs(other.Position.y - Position.y) <= FLOAT_COMPARISON_DELTA &&
                Mathf.Abs(other.Position.z - Position.z) <= FLOAT_COMPARISON_DELTA;
        }
    }
}
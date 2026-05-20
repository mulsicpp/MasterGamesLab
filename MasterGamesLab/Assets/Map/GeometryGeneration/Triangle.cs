using System.Collections.Generic;
using UnityEngine;

namespace Map.GeometryGeneration
{
    public class Triangle
    {
        public Vector3 Center { get; private set; }
        private readonly List<Tile> points;

        public Triangle(Tile p1, Tile p2, Tile p3)
        {
            points = new List<Tile> { p1, p2, p3 };
            Center = UpdateCenter();
            p1.AddNeighborTriangle(this);
            p2.AddNeighborTriangle(this);
            p3.AddNeighborTriangle(this);
        }

        public IReadOnlyList<Tile> Points => points.AsReadOnly();

        private Vector3 UpdateCenter()
        {
            var center = Vector3.zero;
            foreach (var point in Points)
            {
                center += point.Position;
            }

            return center / Points.Count;
        }
    }
}
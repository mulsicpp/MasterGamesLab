using System.Collections.Generic;
using UnityEngine;

namespace GeometryGeneration
{
    public class Triangle
    {
        public Vector3 Center => UpdateCenter();
        private readonly List<Point> points;

        public Triangle(Point p1, Point p2, Point p3)
        {
            points = new List<Point> { p1, p2, p3 };
            p1.AddNeighbor(this);
            p2.AddNeighbor(this);
            p3.AddNeighbor(this);
        }

        public IReadOnlyList<Point> Points => points.AsReadOnly();

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
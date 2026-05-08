using System.Collections.Generic;
using UnityEngine;

namespace GeometryGeneration
{
    public class Face
    {
        private readonly List<Point> points;
        public Vector3 Center;

        public Face(Point p1, Point p2, Point p3)
        {
            points = new List<Point> { p1, p2, p3 };
            p1.AddNeighbor(this);
            p2.AddNeighbor(this);
            p3.AddNeighbor(this);
            UpdateCenter();
        }

        public IReadOnlyList<Point> Points => points.AsReadOnly();

        public void AddPoint(Point point)
        {
            points.Add(point);
            UpdateCenter();
        }

        public void RemovePoint(Point point)
        {
            points.Remove(point);
            UpdateCenter();
        }

        public void ClearPoints()
        {
            points.Clear();
            UpdateCenter();
        }

        private void UpdateCenter()
        {
            Center = Vector3.zero;
            foreach (var point in Points) Center += point.Position;

            Center /= Points.Count;
        }
    }
}
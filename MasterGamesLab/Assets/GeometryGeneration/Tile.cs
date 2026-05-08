using System.Collections.Generic;
using UnityEngine;

namespace GeometryGeneration
{
    public class Tile
    {
        public readonly List<Face> Faces;
        private readonly List<Vector3> neighbourCenters;
        private readonly float sphereRadius;
        private readonly float tileSize;
        private readonly Point center;

        public Tile(Point center, float sphereRadius, float tileSize)
        {
            this.center = center;
            this.sphereRadius = sphereRadius;
            this.tileSize = Mathf.Max(0.01f, Mathf.Min(1f, tileSize));

            Faces = new List<Face>();
            neighbourCenters = new List<Vector3>();
            if (center.Neighbors.Count > 0)
            {
                var neighbors = center.Neighbors;

                var normal = center.Position.normalized;

                var tangent = Vector3.Cross(normal, Vector3.up);
                if (tangent.magnitude < 0.001f) tangent = Vector3.Cross(normal, Vector3.right);

                tangent.Normalize();
                var bitangent = Vector3.Cross(normal, tangent);

                neighbors.Sort((a, b) =>
                {
                    var vA = a.Center - center.Position;
                    var angleA = Mathf.Atan2(Vector3.Dot(vA, bitangent), Vector3.Dot(vA, tangent));
                    var vB = b.Center - center.Position;
                    var angleB = Mathf.Atan2(Vector3.Dot(vB, bitangent), Vector3.Dot(vB, tangent));
                    return angleA.CompareTo(angleB);
                });

                foreach (var face in neighbors) neighbourCenters.Add(face.Center);
            }

            BuildFaces();
        }

        private Vector3 ProjectToSphere(Vector3 position)
        {
            return position.normalized * sphereRadius;
        }

        private void BuildFaces()
        {
            var geometryVertices = new List<Point>();
            foreach (var point in neighbourCenters)
                geometryVertices.Add(new Point(ProjectToSphere(Vector3.Lerp(center.Position, point, tileSize))));

            Faces.Add(new Face(geometryVertices[0], geometryVertices[1], geometryVertices[2]));

            for (var i = 0; i < geometryVertices.Count - 2; i++)
                Faces.Add(new Face(geometryVertices[0], geometryVertices[i + 1], geometryVertices[i + 2]));
        }
    }
}
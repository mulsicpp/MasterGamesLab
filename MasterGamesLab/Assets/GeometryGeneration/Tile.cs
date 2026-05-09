using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryGeneration
{
    public class Tile
    {
        public readonly int Id;
        private readonly List<int> neighbourIds;
        private List<Triangle> neighbourTriangles;
        public List<Triangle> Faces;

        private readonly Point center;
        public readonly Vector3 CenterOnSphere;

        public Vector3 Center
        {
            get => center.Position;
            set => center.Position = value;
        }


        public Tile(Point center, float sphereRadius)
        {
            Id = center.Id;
            this.center = center;
            // Center = ProjectToSphere(Center, sphereRadius);
            CenterOnSphere = Point.ProjectToSphere(Center, sphereRadius);

            // Center = ProjectToSphere(Center, sphereRadius);
            // if (Id == 0)
            // {
            //     Debug.Log("Center: " + Center);
            // }

            if (center.Neighbors.Count == 0)
            {
                throw new System.Exception($"Tile {Id} at {center.Position} has no neighbours");
            }

            neighbourIds = new List<int>(center.Neighbors.Count);
            UpdateNeighbourIdsAndTriangles();
        }

        public void BuildFaces(float tileSize = 1, float maxEdgeLength = 1)
        {
            tileSize = Math.Clamp(tileSize, 0.001f, 1);

            Faces = new List<Triangle>(4);
            var neighbourCenters = new List<Vector3>(neighbourTriangles.Count);
            foreach (var triangle in neighbourTriangles)
            {
                neighbourCenters.Add(triangle.Center);
            }

            var geometryVertices = new List<Point>();
            foreach (var point in neighbourCenters)
            {
                //geometryVertices.Add(new Point(ProjectToSphere(Vector3.Lerp(Center, point, tileSize))));
                geometryVertices.Add(new Point(Vector3.Lerp(Center, point, tileSize)));
            }

            var maxEdgeLengthSqrd = maxEdgeLength * maxEdgeLength;
            for (var i = 0; i < geometryVertices.Count - 2; i++)
            {
                var edge0 = geometryVertices[i + 1].Position - geometryVertices[0].Position;
                var edge1 = geometryVertices[i + 2].Position - geometryVertices[0].Position;
                var edge2 = geometryVertices[i + 2].Position - geometryVertices[i + 1].Position;

                if (edge0.sqrMagnitude > maxEdgeLengthSqrd || edge1.sqrMagnitude > maxEdgeLengthSqrd ||
                    edge2.sqrMagnitude > maxEdgeLengthSqrd)
                {
                    continue;
                }

                Faces.Add(new Triangle(geometryVertices[0], geometryVertices[i + 1], geometryVertices[i + 2]));
            }
        }

        private void UpdateNeighbourIdsAndTriangles()
        {
            neighbourTriangles = center.Neighbors;

            var normal = center.Position.normalized;

            var tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.magnitude < 0.001f)
            {
                tangent = Vector3.Cross(normal, Vector3.right);
            }

            tangent.Normalize();
            var bitangent = Vector3.Cross(normal, tangent);

            neighbourTriangles.Sort((a, b) =>
            {
                var vA = a.Center - center.Position;
                var angleA = Mathf.Atan2(Vector3.Dot(vA, bitangent), Vector3.Dot(vA, tangent));
                var vB = b.Center - center.Position;
                var angleB = Mathf.Atan2(Vector3.Dot(vB, bitangent), Vector3.Dot(vB, tangent));
                return angleA.CompareTo(angleB);
            });

            foreach (var face in neighbourTriangles)
            {
                if (!neighbourIds.Contains(face.Points[0].Id))
                {
                    neighbourIds.Add(face.Points[0].Id);
                }
            }
        }
    }
}
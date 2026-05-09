using System.Collections.Generic;
using UnityEngine;

namespace GeometryGeneration
{
    public class MapMesh
    {
        public MapMesh(List<Vector3> vertices, List<int> triangles)
        {
            Vertices = vertices;
            Triangles = triangles;
        }

        public List<Vector3> Vertices { get; }

        public List<int> Triangles { get; }
    }
}
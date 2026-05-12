using System.Collections.Generic;
using GeometryGeneration.Projections;
using UnityEngine;

namespace Map.GeometryGeneration
{
    public class MapChunk : MonoBehaviour
    {
        private IMap parent;
        private MeshFilter meshFilter;
        private int startIdx;
        private int endIdx;
        private List<Vector3> vertices;
        private List<int> triangles;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        public void Init(IMap parentMap, int startIndex, int endIndex)
        {
            parent = parentMap;
            startIdx = startIndex;
            endIdx = endIndex;
        }

        public void UpdateMesh()
        {
            vertices = new List<Vector3>();
            triangles = new List<int>();

            var vertIdx = 0;
            for (var i = startIdx; i < endIdx; i++)
            {
                var tile = parent.Tiles[i];
                tile.BuildFaces(parent.HexSize);

                foreach (var face in tile.Faces)
                {
                    vertices.Add(face.Points[0].Position);
                    vertices.Add(face.Points[1].Position);
                    vertices.Add(face.Points[2].Position);
                    triangles.AddRange(new[] { vertIdx, vertIdx + 1, vertIdx + 2 });
                    vertIdx += 3;
                }
            }

            var mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray()
            };
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
        }
    }
}
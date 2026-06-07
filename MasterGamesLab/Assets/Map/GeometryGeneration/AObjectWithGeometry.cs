using System.Collections.Generic;
using UnityEngine;

namespace Map.GeometryGeneration
{
    public abstract class AObjectWithGeometry : MonoBehaviour
    {
        private MeshFilter meshFilter;

        protected readonly List<Vector3> Vertices = new List<Vector3>();
        protected readonly List<Vector4> UV1 = new List<Vector4>();
        protected readonly List<int> Triangles = new List<int>();

        protected void Init()
        {
            meshFilter = GetComponent<MeshFilter>();
            if (Mesh != null)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(Mesh);
                }
                else
                {
                    Destroy(Mesh);
                }
            }
            Mesh = new Mesh { name = "Mesh" };
            meshFilter.mesh = Mesh;
        }

        protected virtual void OnDestroy()
        {
            if (Mesh != null)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(Mesh);
                }
                else
                {
                    Destroy(Mesh);
                }
                Mesh = null;
            }
        }

        protected void ClearMeshData()
        {
            Vertices.Clear();
            UV1.Clear();
            Triangles.Clear();
        }

        protected void AddVertex(Vector3 vertex, Vector4 uv)
        {
            Vertices.Add(vertex);
            UV1.Add(uv);
        }

        protected void StoreMeshData()
        {
            Mesh.Clear();
            Mesh.SetVertices(Vertices);
            Mesh.SetUVs(1, UV1);
            Mesh.SetTriangles(Triangles, 0);

            Mesh.RecalculateNormals();
            Mesh.RecalculateBounds();
        }

        protected Mesh Mesh { get; private set; }
    }
}
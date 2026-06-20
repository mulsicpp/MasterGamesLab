using System.Collections.Generic;
using Map.OutlineEffect;
using UnityEngine;

namespace Map.GeometryGeneration
{
    public abstract class AObjectWithProcedualGeometry : AOutlineableObjectBase
    {
        private MeshFilter meshFilter;

        public readonly List<Vector3> Vertices = new List<Vector3>();
        protected readonly List<Vector4> UV1 = new List<Vector4>();
        public readonly List<int> Triangles = new List<int>();
        protected Mesh Mesh { get; private set; }

        protected void Init(bool outlineable = true)
        {
            if (outlineable)
            {
                base.Init();
            }

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

        public void ClearMeshData()
        {
            Vertices.Clear();
            UV1.Clear();
            Triangles.Clear();
        }

        public void AddVertex(Vector3 vertex, Vector4 uv)
        {
            Vertices.Add(vertex);
            UV1.Add(uv);
        }

        public void AddTriangle(int a, int b, int c)
        {
            Triangles.Add(a);
            Triangles.Add(b);
            Triangles.Add(c);
        }

        public void StoreMeshData()
        {
            Mesh.Clear();
            Mesh.SetVertices(Vertices);
            Mesh.SetUVs(1, UV1);
            Mesh.SetTriangles(Triangles, 0);

            Mesh.RecalculateNormals();
            Mesh.RecalculateBounds();
        }
    }
}
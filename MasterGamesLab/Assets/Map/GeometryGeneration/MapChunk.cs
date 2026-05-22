using System.Collections.Generic;
using UnityEngine;

namespace Map.GeometryGeneration
{
    public class MapChunk : MonoBehaviour
    {
        public bool GeometryChanged;
        public bool Dirty;

        private IMap parent;
        private MeshFilter meshFilter;
        private Mesh mesh;
        private int startIdx;
        private int endIdx;
        private List<Vector3> vertices;
        private List<int> triangles;
        private List<Vector4> tileData;
        private List<Vector4> materialData;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        public void Init(IMap parentMap, int startIndex, int endIndex)
        {
            parent = parentMap;
            startIdx = startIndex;
            endIdx = endIndex;
            vertices = new List<Vector3>();
            triangles = new List<int>();
            tileData = new List<Vector4>();
            materialData = new List<Vector4>();
        }

        public void UpdateMesh()
        {
            vertices = new List<Vector3>(vertices.Count);
            triangles = new List<int>(triangles.Count);
            tileData = new List<Vector4>(tileData.Count);
            materialData = new List<Vector4>(materialData.Count);

            // var vertIdx = 0;
            for (var i = startIdx; i < endIdx; i++)
            {
                var tile = parent.Tiles[i];
                tile.BuildFaces(vertices, triangles, tileData, materialData);

                /*var tileData = tile.GetTileData();

                foreach (var face in tile.Faces)
                {
                    vertices.Add(face.Points[0].Position);
                    vertices.Add(face.Points[1].Position);
                    vertices.Add(face.Points[2].Position);
                    triangles.AddRange(new[] { vertIdx, vertIdx + 1, vertIdx + 2 });

                    this.tileData.Add(tileData);
                    this.tileData.Add(tileData);
                    this.tileData.Add(tileData);
                    vertIdx += 3;
                }*/
            }

            mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray()
            };
            mesh.RecalculateNormals();
            mesh.SetUVs(1, tileData);
            mesh.SetUVs(2, materialData);
            meshFilter.mesh = mesh;
            GeometryChanged = false;
            Dirty = false;
        }

        public void UpdateTileData()
        {
            /*tileData = new List<Vector4>(tileData.Count);

            for (var i = startIdx; i < endIdx; i++)
            {
                var tile = parent.Tiles[i];
                var tileData = tile.GetTileData();

                foreach (var face in tile.Faces)
                {
                    this.tileData.Add(tileData);
                    this.tileData.Add(tileData);
                    this.tileData.Add(tileData);
                }
            }

            mesh.SetUVs(1, tileData);*/
            Dirty = false;
        }
    }
}
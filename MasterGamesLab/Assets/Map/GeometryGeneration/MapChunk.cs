using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Map.GeometryGeneration
{
    public class MapChunk : MonoBehaviour
    {
        private static readonly int TreeBuffer = Shader.PropertyToID("_TreeBuffer");

        public struct ChunkGeometry
        {
            public List<Vector3> Vertices;
            public List<int> Triangles;
            public List<Vector4> TileData;
            public List<Vector4> MaterialData;
            public List<Map.TreeData> TreeData;
        }

        public struct TileGeometryInformation
        {
            public int NumVertices;
            public int StartTreeIdx;
            public int EndTreeIdx;
        }

        [SerializeField] private Mesh treeMesh;
        [SerializeField] private Material treeMaterial;

        public bool GeometryChanged;
        public bool Dirty;
        public IMap Parent;

        private MeshFilter meshFilter;
        private Mesh mesh;
        private int startIdx;
        private int endIdx;
        private List<Vector3> vertices;
        private List<int> triangles;
        private List<Vector4> tileData;
        private List<Vector4> materialData;

        private List<Map.TreeData> treeData;
        private GraphicsBuffer treeBuffer;
        private Bounds renderBounds;
        private MaterialPropertyBlock mpb;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        public void Init(IMap parentMap, int startIndex, int endIndex)
        {
            Parent = parentMap;
            startIdx = startIndex;
            endIdx = endIndex;
            vertices = new List<Vector3>();
            triangles = new List<int>();
            tileData = new List<Vector4>();
            materialData = new List<Vector4>();
            renderBounds = new Bounds(Vector3.zero,
                new Vector3(Parent.Radius * 4, Parent.Radius * 4, Parent.Radius * 4));
            treeData = new List<Map.TreeData>();
        }

        public void UpdateMesh()
        {
            vertices = new List<Vector3>(vertices.Count);
            triangles = new List<int>(triangles.Count);
            tileData = new List<Vector4>(tileData.Count);
            materialData = new List<Vector4>(materialData.Count);
            treeData = new List<Map.TreeData>();

            for (var i = startIdx; i < endIdx; i++)
            {
                var tile = Parent.Tiles[i];
                tile.BuildFaces(new ChunkGeometry
                {
                    Vertices = vertices,
                    Triangles = triangles,
                    TileData = tileData,
                    MaterialData = materialData,
                    TreeData = treeData,
                });
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

            SetTreeBuffer();

            GeometryChanged = false;
            Dirty = false;
        }

        public void UpdateTileData()
        {
            tileData = new List<Vector4>(tileData.Count);

            for (var i = startIdx; i < endIdx; i++)
            {
                var tile = Parent.Tiles[i];
                tile.FillTileData(tileData, treeData);
            }

            mesh.SetUVs(1, tileData);
            SetTreeBuffer();
            Dirty = false;
        }

        public void RenderTrees()
        {
            if (treeBuffer == null)
            {
                return;
            }

            // 1. Create the new RenderParams struct and assign your material
            var renderParams = new RenderParams(treeMaterial)
            {
                // 2. Assign the massive bounds we calculated in Start() 
                // (This is crucial so Unity doesn't cull the trees when unrolled)
                worldBounds = renderBounds,
                matProps = mpb,
                shadowCastingMode = ShadowCastingMode.On,
                receiveShadows = false,
            };

            // 3. Draw the meshes using the modern Unity 6 API
            Graphics.RenderMeshPrimitives(renderParams, treeMesh, 0, treeBuffer.count);
        }

        private void SetTreeBuffer()
        {
            if (treeBuffer != null)
            {
                treeBuffer.Release();
                treeBuffer = null;
            }
            
            if (treeData == null || treeData.Count == 0)
            {
                return;
            }

            mpb = new MaterialPropertyBlock();

            var stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Map.TreeData));
            treeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, treeData.Count, stride);
            treeBuffer.SetData(treeData);
            mpb.SetBuffer(TreeBuffer, treeBuffer);
        }
    }
}
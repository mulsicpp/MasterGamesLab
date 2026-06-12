using System.Collections.Generic;
using Map.GeometryGeneration.Edges;
using UnityEngine;
using UnityEngine.Rendering;

namespace Map.GeometryGeneration
{
    public class MapChunk : AObjectWithProcedualGeometry
    {
        private static readonly int TreeBuffer = Shader.PropertyToID("_TreeBuffer");
        protected override string OutlineLayerName() => "";
        protected override string OutlineTransparentLayerName() => "";

        public struct ChunkGeometry
        {
            public List<Vector3> Vertices;
            public List<int> Triangles;
            public List<Vector4> TileData;
            public List<Vector4> MaterialData;
            public List<Map.TreeData> TreeData;

            public static ChunkGeometry Empty => new()
            {
                Vertices = new List<Vector3>(), Triangles = new List<int>(), TileData = new List<Vector4>(),
                MaterialData = new List<Vector4>(), TreeData = new List<Map.TreeData>()
            };
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

        private int startIdx;
        private int endIdx;
        private List<Vector4> materialData;

        private List<Map.TreeData> treeData;
        private GraphicsBuffer treeBuffer;
        private Bounds renderBounds;
        private MaterialPropertyBlock mpb;

        private void Awake() => Init(false);

        public void Init(IMap parentMap, int startIndex, int endIndex)
        {
            Parent = parentMap;
            startIdx = startIndex;
            endIdx = endIndex;
            ClearMeshData();
            materialData = new List<Vector4>();
            renderBounds = new Bounds(Vector3.zero,
                new Vector3(Parent.Radius * 4, Parent.Radius * 4, Parent.Radius * 4));
            treeData = new List<Map.TreeData>();
        }

        public void UpdateMesh()
        {
            ClearMeshData();
            materialData = new List<Vector4>(materialData.Count);
            treeData = new List<Map.TreeData>();

            for (var i = startIdx; i < endIdx; i++)
            {
                var tile = Parent.Tiles[i];
                tile.BuildFaces(new ChunkGeometry
                {
                    Vertices = Vertices,
                    Triangles = Triangles,
                    TileData = UV1,
                    MaterialData = materialData,
                    TreeData = treeData,
                });
            }

            StoreMeshData();
            Mesh.SetUVs(2, materialData);
            SetTreeBuffer();

            GeometryChanged = false;
            Dirty = false;
        }

        public void UpdateTileData()
        {
            UV1.Clear();

            for (var i = startIdx; i < endIdx; i++)
            {
                var tile = Parent.Tiles[i];
                tile.FillTileData(UV1, treeData);
            }

            Mesh.SetUVs(1, UV1);
            SetTreeBuffer();
            Dirty = false;
        }

        public void RenderTrees()
        {
            if (treeBuffer == null || treeMesh == null || treeMaterial == null)
            {
                return;
            }

            var renderParams = new RenderParams(treeMaterial)
            {
                worldBounds = renderBounds,
                matProps = mpb,
                shadowCastingMode = ShadowCastingMode.On,
                receiveShadows = false,
            };

            Graphics.RenderMeshPrimitives(renderParams, treeMesh, 0, treeBuffer.count);
        }

        public EdgeGeometry RequestNewEdgeGeometry()
        {
            var edgesGameObject = Instantiate(Map.Instance.GetEdgeGeometryPrefab(), transform);
            return edgesGameObject.GetComponent<EdgeGeometry>();
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

            if (mpb == null)
            {
                mpb = new MaterialPropertyBlock();
            }

            var stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Map.TreeData));
            treeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, treeData.Count, stride);
            treeBuffer.SetData(treeData);
            mpb.SetBuffer(TreeBuffer, treeBuffer);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (treeBuffer != null)
            {
                treeBuffer.Release();
                treeBuffer = null;
            }
        }
    }
}
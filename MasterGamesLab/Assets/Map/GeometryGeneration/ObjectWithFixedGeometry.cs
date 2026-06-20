using System.Collections.Generic;
using Map.OutlineEffect;
using UnityEngine;

namespace Map.GeometryGeneration
{
    public class ObjectWithFixedGeometry : AOutlineableObjectBase
    {
        private MeshFilter meshFilter;
        private List<Vector4> uv1 = new List<Vector4>();

        protected override string OutlineLayerName() => outlineLayerName;
        protected override string OutlineTransparentLayerName() => outlineTransparentLayerName;

        private string outlineLayerName;
        private string outlineTransparentLayerName;

        public void Init(Mesh mesh, string defaultLayerName, string outlineName, string outlineTransparentName, int id)
        {
            outlineLayerName = outlineName;
            outlineTransparentLayerName = outlineTransparentName;

            base.Init(defaultLayerName);

            meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            FillUv1(id);
        }

        private void FillUv1(int id)
        {
            var mesh = meshFilter.mesh;
            var count = mesh.vertexCount;

            uv1 = new List<Vector4>(count);
            var uvValue = new Vector4(id + Map.ID_OFFSET, 0, 0, 0);

            for (var i = 0; i < count; i++)
            {
                uv1.Add(uvValue);
            }

            mesh.SetUVs(1, uv1);
        }

        public void UpdateId(int id)
        {
            var uvValue = new Vector4(id + Map.ID_OFFSET, 0, 0, 0);

            for (var i = 0; i < uv1.Count; i++)
            {
                uv1[i] = uvValue;
            }
            meshFilter.mesh.SetUVs(1, uv1);
        }
    }
}
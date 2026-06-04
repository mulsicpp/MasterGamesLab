using System.Collections.Generic;
using UnityEngine;

namespace Map.GeometryGeneration.Edges
{
    public class EdgeGeometry : MonoBehaviour
    {
        private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int InnerColor = Shader.PropertyToID("_InnerColor");
        private static readonly int TextureId = Shader.PropertyToID("_TextureId");
        private static readonly int RoadColor = Shader.PropertyToID("_PlayerColor");

        public static int DefaultLayer = -1;
        public static int OutlineLayer;
        public static int OutlineTransparentLayer;

        private Color outlineColor;
        private Color innerColor;
        private int textureId;
        private Color roadColor;

        private Renderer objRenderer;

        private Mesh mesh;
        private MeshFilter meshFilter;

        private List<Vector3> vertices = new List<Vector3>();
        private List<Vector4> uv1 = new List<Vector4>();
        private List<int> triangles = new List<int>();

        private Edge.PartialEdgeGeometry? startGeometry;
        private Edge.PartialEdgeGeometry? endGeometry;

        private void Awake()
        {
            if (DefaultLayer == -1)
            {
                DefaultLayer = gameObject.layer;
                OutlineLayer = LayerMask.NameToLayer("Edge Outline");
                OutlineTransparentLayer = LayerMask.NameToLayer("Edge Outline Transparent");
            }

            objRenderer = GetComponent<Renderer>();
            meshFilter = GetComponent<MeshFilter>();

            mesh = new Mesh { name = "CombinedEdgeMesh" };
            meshFilter.sharedMesh = mesh;

            // Apply initial material properties
            SetMaterialPropertyBlock();
        }

        public void SetStartMesh(Edge.PartialEdgeGeometry newStartGeometry)
        {
            startGeometry = newStartGeometry;
            RebuildMesh();
        }

        public void SetEndMesh(Edge.PartialEdgeGeometry newEndGeometry)
        {
            endGeometry = newEndGeometry;
            RebuildMesh();
        }

        public void SetLayer(int layer) => gameObject.layer = layer;

        public void SetRoadColor(Color color)
        {
            roadColor = color;
            SetMaterialPropertyBlock();
        }

        public void SetOutlineParameters(Color colorOutline, Color colorInner, int outlineTextureId)
        {
            outlineColor = colorOutline;
            innerColor = colorInner;
            textureId = outlineTextureId;
            SetMaterialPropertyBlock();
        }

        public void SetOutlineParameters(Constants.OutlineData outlineData)
        {
            outlineColor = outlineData.OutlineColor;
            innerColor = outlineData.InnerColor;
            textureId = outlineData.TextureId;
            SetMaterialPropertyBlock();
        }

        private void RebuildMesh()
        {
            vertices.Clear();
            uv1.Clear();
            triangles.Clear();

            var vertexOffset = 0;

            if (startGeometry is { Vertices: not null })
            {
                vertices.AddRange(startGeometry.Value.Vertices);
                uv1.AddRange(startGeometry.Value.UV1);

                foreach (var tri in startGeometry.Value.Triangles)
                {
                    triangles.Add(tri);
                }

                vertexOffset += startGeometry.Value.Vertices.Count;
            }

            if (endGeometry is { Vertices: not null })
            {
                vertices.AddRange(endGeometry.Value.Vertices);
                uv1.AddRange(endGeometry.Value.UV1);

                foreach (var tri in endGeometry.Value.Triangles)
                {
                    triangles.Add(tri + vertexOffset);
                }
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(1, uv1);
            mesh.SetTriangles(triangles, 0);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        private void SetMaterialPropertyBlock()
        {
            if (objRenderer == null)
            {
                Debug.LogError("Renderer is null");
                return;
            }

            var mpb = new MaterialPropertyBlock();
            objRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(RoadColor, roadColor);
            mpb.SetColor(OutlineColor, outlineColor);
            mpb.SetColor(InnerColor, innerColor);
            mpb.SetFloat(TextureId, textureId);
            objRenderer.SetPropertyBlock(mpb);
        }
    }
}
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

        public static int defaultLayer = -1;
        public static int outlineLayer;
        public static int outlineTransparentLayer;

        private Color outlineColor;
        private Color innerColor;
        private int textureId;
        private Color roadColor;

        private Renderer objRenderer;

        private Mesh mesh;
        private MeshFilter meshFilter;

        private List<Vector3> vertices = new List<Vector3>();
        private List<int> triangles = new List<int>();

        private Edge.PartialEdgeGeometry? startGeometry;
        private Edge.PartialEdgeGeometry? endGeometry;

        private void Awake()
        {
            if (defaultLayer == -1)
            {
                defaultLayer = gameObject.layer;
                outlineLayer = LayerMask.NameToLayer("Outline");
                outlineTransparentLayer = LayerMask.NameToLayer("Outline Transparent");
            }

            objRenderer = GetComponent<Renderer>();
            meshFilter = GetComponent<MeshFilter>();

            mesh = new Mesh { name = "CombinedEdgeMesh" };
            meshFilter.sharedMesh = mesh;

            // Apply initial material properties
            SetMaterialPropertyBlock();
        }

        public void SetStartMesh(Edge.PartialEdgeGeometry startGeometry)
        {
            this.startGeometry = startGeometry;
            RebuildMesh();
        }

        public void SetEndMesh(Edge.PartialEdgeGeometry endGeometry)
        {
            this.endGeometry = endGeometry;
            RebuildMesh();
        }

        public void BuildRoads(Tile tile)
        {
            vertices = new List<Vector3>();
            triangles = new List<int>();

            var d = tile.NeighborTiles[0];
            var center = tile.PositionOnSphere;
            var edgeMidpoint = (d.LeftVertex + d.RightVertex) / 2;

            var up = center.normalized;
            var forward = (center - edgeMidpoint).normalized;
            var right = Vector3.Cross(up, forward).normalized;

            float width = Vector3.Distance(d.LeftVertex, d.RightVertex) * 0.2f;
            float thickness = 0.05f;
            float length = Vector3.Distance(center, edgeMidpoint);

            // Cube vertices (8 corners)
            // Near face (at edgeMidpoint)
            Vector3 v0 = edgeMidpoint - right * width + up * thickness;
            Vector3 v1 = edgeMidpoint + right * width + up * thickness;
            Vector3 v2 = edgeMidpoint + right * width - up * thickness;
            Vector3 v3 = edgeMidpoint - right * width - up * thickness;

            // Far face (at center)
            Vector3 v4 = center - right * width + up * thickness;
            Vector3 v5 = center + right * width + up * thickness;
            Vector3 v6 = center + right * width - up * thickness;
            Vector3 v7 = center - right * width - up * thickness;

            vertices.AddRange(new[] { v0, v1, v2, v3, v4, v5, v6, v7 });

            // 6 faces, 2 triangles each
            triangles.AddRange(new[]
            {
                // Front (Near)
                0, 1, 2, 0, 2, 3,
                // Back (Far)
                4, 6, 5, 4, 7, 6,
                // Top
                0, 4, 5, 0, 5, 1,
                // Bottom
                2, 6, 7, 2, 7, 3,
                // Left
                0, 3, 7, 0, 7, 4,
                // Right
                1, 5, 6, 1, 6, 2
            });

            mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray()
            };
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
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
            triangles.Clear();

            var vertexOffset = 0;

            if (startGeometry is { Vertices: not null })
            {
                vertices.AddRange(startGeometry.Value.Vertices);

                foreach (var tri in startGeometry.Value.Triangles)
                {
                    triangles.Add(tri);
                }

                vertexOffset += startGeometry.Value.Vertices.Count;
            }

            if (endGeometry is { Vertices: not null })
            {
                vertices.AddRange(endGeometry.Value.Vertices);

                foreach (var tri in endGeometry.Value.Triangles)
                {
                    triangles.Add(tri + vertexOffset);
                }
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
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
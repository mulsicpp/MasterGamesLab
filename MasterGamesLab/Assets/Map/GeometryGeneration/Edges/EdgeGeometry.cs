using System.Collections.Generic;
using Map.OutlineEffect;
using UnityEngine;

namespace Map.GeometryGeneration.Edges
{
    public class EdgeGeometry : AOutlineableEdge
    {
        private Edge.PartialEdgeGeometry? startGeometry;
        private Edge.PartialEdgeGeometry? endGeometry;

        private void Awake() => Init();

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

        private void RebuildMesh()
        {
            ClearMeshData();

            var vertexOffset = 0;

            if (startGeometry is { Vertices: not null })
            {
                Vertices.AddRange(startGeometry.Value.Vertices);
                UV1.AddRange(startGeometry.Value.UV1);

                foreach (var tri in startGeometry.Value.Triangles)
                {
                    Triangles.Add(tri);
                }

                vertexOffset += startGeometry.Value.Vertices.Count;
            }

            if (endGeometry is { Vertices: not null })
            {
                Vertices.AddRange(endGeometry.Value.Vertices);
                UV1.AddRange(endGeometry.Value.UV1);

                foreach (var tri in endGeometry.Value.Triangles)
                {
                    Triangles.Add(tri + vertexOffset);
                }
            }

            StoreMeshData();
        }
    }
}
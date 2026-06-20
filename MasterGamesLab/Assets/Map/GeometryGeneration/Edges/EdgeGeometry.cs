namespace Map.GeometryGeneration.Edges
{
    public class EdgeGeometry : AObjectWithProcedualGeometry
    {
        protected override string DefaultLayerName() => defaultLayerName;
        protected override string OutlineLayerName() => outlineLayerName;
        protected override string OutlineTransparentLayerName() => outlineTransparentLayerName;

        private Edge.PartialEdgeGeometry? startGeometry;
        private Edge.PartialEdgeGeometry? endGeometry;

        private string defaultLayerName = "Edge";
        private string outlineLayerName = "Edge Outline";
        private string outlineTransparentLayerName = "Edge Outline Transparent";

        private void Awake() => Init();

        public void SetLayerNames(string newDefaultLayerName, string newOutlineLayerName,
            string newOutlineTransparentLayerName)
        {
            defaultLayerName = newDefaultLayerName;
            outlineLayerName = newOutlineLayerName;
            outlineTransparentLayerName = newOutlineTransparentLayerName;
            Init();
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
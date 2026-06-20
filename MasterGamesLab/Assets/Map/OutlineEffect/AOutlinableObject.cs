using Map.GeometryGeneration;

namespace Map.OutlineEffect
{
    public class AOutlinableObject : AObjectWithProcedualGeometry
    {
        protected override string DefaultLayerName() => "Default";

        protected override string OutlineLayerName() => "Outline";

        protected override string OutlineTransparentLayerName() => "Outline Transparent";
    }
}
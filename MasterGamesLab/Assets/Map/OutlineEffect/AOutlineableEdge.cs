namespace Map.OutlineEffect
{
    public class AOutlineableEdge : AOutlineableObjectBase
    {
        protected override string OutlineLayerName() => "Edge Outline";

        protected override string OutlineTransparentLayerName() => "Edge Outline Transparent";
    }
}
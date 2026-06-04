namespace Map.OutlineEffect
{
    public class AOutlinableObject : AOutlineableObjectBase
    {
        protected override string OutlineLayerName() => "Outline";

        protected override string OutlineTransparentLayerName() => "Outline Transparent";
    }
}
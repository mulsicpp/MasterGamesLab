using Map.OutlineEffect;

namespace Map.Hoverables
{
    public interface IHoverable : IOutlinable
    {
        public void ShowHoverOutline(HoverState hoverState = HoverState.Valid);
    }
}
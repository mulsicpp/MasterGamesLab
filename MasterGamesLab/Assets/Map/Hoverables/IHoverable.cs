namespace Map.Hoverables
{
    public interface IHoverable : IMapEntity
    {
        public void ShowHoverOutline(HoverState hoverState = HoverState.Valid);
        public void SetHoverableStatus(bool isHoverable);
    }
}
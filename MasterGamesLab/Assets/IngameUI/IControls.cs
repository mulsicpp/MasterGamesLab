using Map.Hoverables;

namespace UI
{
    public interface IControls : IClickEventHandler
    {
        public bool ControlsAreActive { get; }
        public void DisableControls();
        public HoverablePicker.HoverableLayer SelectHoverableLayers();

        public void UpdateControls();
    }
}
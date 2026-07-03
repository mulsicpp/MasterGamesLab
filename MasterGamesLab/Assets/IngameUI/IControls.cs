using Map.Hoverables;
using System;

namespace UI
{
    public interface IControls : IClickEventHandler
    {
        public bool ControlsAreActive { get; }
        public void DisableControls();
        public Predicate<IHoverable> GetHoverablePredicate();

        public void UpdateControls();
    }
}
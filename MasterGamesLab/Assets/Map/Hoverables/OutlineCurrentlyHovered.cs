using Map.Hoverables;
using UnityEngine;

namespace Map.Hoverables
{
    public class OutlineCurrentlyHovered : MonoBehaviour
    {
        private IHoverable previouslyHovered;

        public HoverState HoverState;

        public void Start()
        {
            HoverState = HoverState.Valid;
        }

        private void LateUpdate()
        {
            if (previouslyHovered == Map.Instance.CurrentlyHovered) return;
            previouslyHovered?.ClearOutline();
            Map.Instance.CurrentlyHovered?.ShowHoverOutline(HoverState);

            previouslyHovered = Map.Instance.CurrentlyHovered;
        }
    }
}
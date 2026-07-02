using Map.Hoverables;
using UnityEngine;

namespace Map.Hoverables
{
    public class OutlineCurrentlyHovered : MonoBehaviour
    {
        private IHoverable previouslyHovered;

        private HoverState hoverState;
        public HoverState HoverState
        {
            get => hoverState;
            set
            {
                if(value != hoverState)
                {
                    UpdateOutline(value);
                    hoverState = value;
                }
            }
        }

        public void Start()
        {
            HoverState = HoverState.Valid;
        }

        private void LateUpdate()
        {
            UpdateOutline(HoverState);
        }

        private void UpdateOutline(HoverState state)
        {
            previouslyHovered?.ClearOutline();
            Map.Instance.CurrentlyHovered?.ShowHoverOutline(state);

            previouslyHovered = Map.Instance.CurrentlyHovered;
        }
    }
}
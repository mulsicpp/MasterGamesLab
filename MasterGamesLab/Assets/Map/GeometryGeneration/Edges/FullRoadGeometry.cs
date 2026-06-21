using System;
using Map.Hoverables;

namespace Map.GeometryGeneration.Edges
{
    public class FullRoadGeometry : AObjectWithProcedualGeometry, IHoverable
    {
        public enum FullRoadType
        {
            Cheapest = 0,
            Fastest = 1,
        }

        protected override string DefaultLayerName() => "Full Road";
        protected override string OutlineLayerName() => "Full Road Outline";
        protected override string OutlineTransparentLayerName() => "Full Road Outline Transparent";

        public EntityId EntityId { get; private set; }

        private FullRoadType type;

        private void Awake()
        {
            Init();
            SetOutlineTransparentLayer();
            ClearOutline();
        }

        public void Init(FullRoadType newType)
        {
            type = newType;
            EntityId = new EntityId(Map.Instance.EntityIdManager.FullRoadRange.Start.Value + (int)type);
            Map.Instance.EntityIdManager[EntityId] = this;
            ClearOutline();
        }

        // Clears only the hovered outline
        public void ClearOutline()
        {
            var outline = type switch
            {
                FullRoadType.Cheapest => Constants.CHEAPEST_ROAD_OUTLINE,
                FullRoadType.Fastest => Constants.FASTEST_ROAD_OUTLINE,
                _ => Constants.TRANSPARENT_OUTLINE,
            };

            SetOutlineParameters(outline);
        }

        public void ShowOutline(Constants.OutlineData outlineData)
        {
            SetOutlineParameters(outlineData);
        }

        public void ShowHoverOutline(HoverState hoverState = HoverState.Valid)
        {
            var outlineData = hoverState switch
            {
                HoverState.Invalid => Constants.ROAD_BLUEPRINT_INVALID_OUTLINE,
                _ => Constants.HOVER_OUTLINE_FILLED_IN,
            };

            SetOutlineParameters(outlineData);
        }
    }
}
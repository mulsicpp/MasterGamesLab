using System;
using Map.Hoverables;
using UnityEngine;

namespace Map.GeometryGeneration.Edges
{
    public class RouteGeometry : AObjectWithProcedualGeometry, IHoverable
    {
        public enum RouteType
        {
            Cheapest = 0,
            Fastest = 1,
        }

        protected override string DefaultLayerName() => "Full Road";
        protected override string OutlineLayerName() => "Full Road Outline";
        protected override string OutlineTransparentLayerName() => "Full Road Outline Transparent";

        public EntityId EntityId { get; private set; }

        public RouteType Type { get; private set; }

        private bool hovered = false;

        private void Awake()
        {
            Init();
            SetOutlineTransparentLayer();
            ClearOutline();
        }

        public void Init(RouteType newType)
        {
            Type = newType;
            EntityId = new EntityId(Map.Instance.EntityIdManager.SelectableRouteRange.Start.Value + (int)Type);
            Map.Instance.EntityIdManager[EntityId] = this;
            ClearOutline();
        }

        public void Update()
        {
            transform.localScale = Vector3.one * (hovered ? 1.001f : 1.0f);
        }

        // Clears only the hovered outline
        public void ClearOutline()
        {
            var outline = Type switch
            {
                RouteType.Cheapest => Constants.CHEAPEST_ROAD_OUTLINE,
                RouteType.Fastest => Constants.FASTEST_ROAD_OUTLINE,
                _ => Constants.TRANSPARENT_OUTLINE,
            };

            SetOutlineParameters(outline);
            hovered = false;
        }

        public void ShowOutline(Constants.OutlineData outlineData)
        {
            SetOutlineParameters(outlineData);
            hovered = true;
        }

        public void ShowHoverOutline(HoverState hoverState = HoverState.Valid)
        {
            var outlineData = Type switch
            {
                RouteType.Cheapest => Constants.CHEAPEST_ROAD_OUTLINE_HOVERED,
                _ => Constants.FASTEST_ROAD_OUTLINE_HOVERED,
            };

            SetOutlineParameters(outlineData);
            hovered = true;
        }
    }
}
using System;
using Map.Hoverables;
using UI;
using UnityEngine;

namespace Map.GeometryGeneration.Edges
{
    public class RouteGeometry : AObjectWithProcedualGeometry, IHoverable
    {
        public EntityId EntityId { get; private set; }

        public Route.RouteType Type { get; private set; }

        private bool hovered = false;

        private void Awake()
        {
            Init();
            SetOutlineTransparentLayer();
            ClearOutline();
        }

        public void Init(Route.RouteType newType, int index)
        {
            Type = newType;
            switch (Type)
            {
                case Route.RouteType.Cheapest:
                case Route.RouteType.Fastest:
                    EntityId = new EntityId(Map.Instance.EntityIdManager.SelectableRouteRange.Start.Value + (int)Type);
                    Map.Instance.EntityIdManager[EntityId] = this;
                    break;
                case Route.RouteType.Queued:
                    EntityId = new EntityId(Map.Instance.EntityIdManager.VehicleActionQueueRange.Start.Value + index);
                    break;
                default:
                    EntityId = new EntityId(-1);
                    CurrentlyHoverable = false;
                    break;
            }

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
                Route.RouteType.Cheapest => Constants.CHEAPEST_ROUTE_OUTLINE,
                Route.RouteType.Fastest => Constants.FASTEST_ROUTE_OUTLINE,
                Route.RouteType.Queued => Constants.QUEUED_ROUTE_OUTLINE,
                Route.RouteType.Current => Constants.CURRENT_ROUTE_OUTLINE,
                Route.RouteType.CheapestPreview => Constants.CHEAPEST_ROUTE_PREVIEW_OUTLINE,
                Route.RouteType.FastestPreview => Constants.FASTEST_ROUTE_PREVIEW_OUTLINE,
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
                Route.RouteType.Cheapest => Constants.CHEAPEST_ROUTE_OUTLINE_HOVERED,
                Route.RouteType.Fastest => Constants.FASTEST_ROUTE_OUTLINE_HOVERED,
                Route.RouteType.Queued => Constants.QUEUED_ROUTE_OUTLINE_HOVERED,
                Route.RouteType.Current => Constants.CURRENT_ROUTE_OUTLINE_HOVERED,
                Route.RouteType.CheapestPreview => Constants.CHEAPEST_ROUTE_PREVIEW_OUTLINE_HOVERED,
                Route.RouteType.FastestPreview => Constants.FASTEST_ROUTE_PREVIEW_OUTLINE_HOVERED,
                _ => Constants.TRANSPARENT_OUTLINE,
            };

            SetOutlineParameters(outlineData);
            hovered = true;
        }

        public void SetHoverableStatus(bool isHoverable)
        {
            // if (Type is Route.RouteType.Current or Route.RouteType.CheapestPreview or Route.RouteType.FastestPreview)
            // {
            //     CurrentlyHoverable = false;
            //     return;
            // }
            // CurrentlyHoverable = isHoverable;
        }
    }
}
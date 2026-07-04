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
                Route.RouteType.Cheapest => GeometriesManager.Instance.routeCheapestOutline,
                Route.RouteType.Fastest => GeometriesManager.Instance.routeFastestOutline,
                Route.RouteType.Queued => GeometriesManager.Instance.routeQueuedOutline,
                Route.RouteType.Current => GeometriesManager.Instance.routeCurrentOutline,
                Route.RouteType.CheapestPreview => GeometriesManager.Instance.routeCheapestPreviewOutline,
                Route.RouteType.FastestPreview => GeometriesManager.Instance.routeFastestPreviewOutline,
                _ => Constants.TransparentOutline,
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
            var outline = Type switch
            {
                Route.RouteType.Cheapest => GeometriesManager.Instance.routeCheapestOutline,
                Route.RouteType.Fastest => GeometriesManager.Instance.routeFastestOutline,
                Route.RouteType.Queued => GeometriesManager.Instance.routeQueuedOutline,
                Route.RouteType.Current => GeometriesManager.Instance.routeCurrentOutline,
                Route.RouteType.CheapestPreview => GeometriesManager.Instance.routeCheapestPreviewOutline,
                Route.RouteType.FastestPreview => GeometriesManager.Instance.routeFastestPreviewOutline,
                _ => Constants.TransparentOutline,
            };

            outline.outlineColor.a = 1.0f;
            outline.innerColor.a = 1.0f;
            SetOutlineParameters(outline);
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
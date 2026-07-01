
using Map;
using Map.Fleet;
using Map.GeometryGeneration;
using Map.GeometryGeneration.Edges;
using Map.Hoverables;
using System.Collections.Generic;
using UnityEngine;
using static Map.Fleet.Vehicle;

namespace UI
{
    public class VehicleActionRenderer : MonoBehaviour, IHoverable
    {
        public int ActionIndex { get; private set; }
        public ObjectWithFixedGeometry FixedGeometry { get; private set; } = null;
        public RouteGeometry RouteGeometry { get; private set; } = null;

        public EntityId EntityId => new(Map.Map.Instance.EntityIdManager.VehicleActionQueueRange.Start.Value + ActionIndex);

        public void Init(int actionIndex, Vehicle vehicle, LinkedListNode<VehicleAction> actionNode)
        {
            var action = actionNode.Value;
            ActionIndex = actionIndex;

            var map = Map.Map.Instance;

            Tile startTile, targetTile;

            switch (action.Type)
            {
                case VehicleAction.ActionType.DriveRoute:

                    ParametricCurve.CurveData curveData = vehicle.Type switch
                    {
                        VehicleType.Freighter => ParametricCurve.CurveData.DefaultWaterCurve,
                        _ => ParametricCurve.CurveData.DefaultRoadCurve,
                    };

                    RouteGeometry = EdgeGeometryFactory.GenerateRoute(action.RouteIds, Route.RouteType.Queued, ActionIndex, curveData);
                    RouteGeometry.transform.parent = transform;
                    break;
                case VehicleAction.ActionType.LoadTruck:
                case VehicleAction.ActionType.WaitForTruck:
                    FixedGeometry = GeometriesManager.Instance.GetGameObjectGeometry(GeometriesManager.GeometryType.Truck, EntityId, transform, Player.Player.Self);
                    startTile = vehicle.GetTileLocationAfterAction(actionNode.Previous, out _);
                    targetTile = map.Tiles[action.TargetTileId] as Tile;

                    transform.localPosition = (startTile.PositionOnSphere + targetTile.PositionOnSphere) * 0.5f * 1.02f;
                    transform.localRotation = Quaternion.LookRotation((targetTile.PositionOnSphere - startTile.PositionOnSphere).normalized, transform.localPosition.normalized);
                    break;

                case VehicleAction.ActionType.UnloadTruck:
                    FixedGeometry = GeometriesManager.Instance.GetGameObjectGeometry(GeometriesManager.GeometryType.Truck, EntityId, transform, Player.Player.Self);
                    startTile = map.Tiles[action.TargetTileId] as Tile;

                    transform.localPosition = startTile.PositionOnSphere * 1.05f;
                    transform.localRotation = Quaternion.LookRotation((startTile.NeighborTiles[0].LeftVertex - startTile.PositionOnSphere).normalized, transform.localPosition.normalized);
                    break;
            }

            map.EntityIdManager[EntityId] = this;
        }

        public void UnregisterFromEntities()
        {
            Map.Map.Instance.EntityIdManager[EntityId] = null;
        }

        public void OnDisable()
        {
            FixedGeometry = null;
            RouteGeometry = null;
        }

        public void ClearOutline()
        {
            FixedGeometry?.SetBaseLayer();

            RouteGeometry?.ClearOutline();
        }

        public void ShowOutline(Constants.OutlineData outlineData)
        {
            FixedGeometry?.SetOutlineLayer();
            FixedGeometry?.SetOutlineParameters(outlineData);

            RouteGeometry?.ShowOutline(outlineData);
        }

        public void ShowHoverOutline(HoverState hoverState = HoverState.Valid)
        {
            var outlineData = hoverState switch
            {
                HoverState.Invalid => Constants.ROAD_BLUEPRINT_INVALID_OUTLINE,
                _ => Constants.HOVER_OUTLINE,
            };
            FixedGeometry?.SetOutlineLayer();
            FixedGeometry?.SetOutlineParameters(outlineData);

            RouteGeometry?.ShowHoverOutline(hoverState);
        }
    }
}
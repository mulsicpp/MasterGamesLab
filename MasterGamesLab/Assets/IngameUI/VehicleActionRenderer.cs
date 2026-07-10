using System;
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

        public EntityId EntityId =>
            new(Map.Map.Instance.EntityIdManager.VehicleActionQueueRange.Start.Value + ActionIndex);

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

                    RouteGeometry = EdgeGeometryFactory.GenerateRoute(action.RouteIds, Route.RouteType.Queued,
                        ActionIndex, curveData);
                    RouteGeometry.transform.parent = transform;
                    break;
                case VehicleAction.ActionType.LoadTruck:
                case VehicleAction.ActionType.WaitForTruck:
                    var geometryType = action.Type == VehicleAction.ActionType.LoadTruck
                        ? GeometriesManager.GeometryType.ActionLoad
                        : GeometriesManager.GeometryType.ActionWait;
                    FixedGeometry =
                        GeometriesManager.Instance.GetGameObjectGeometry(geometryType, EntityId, transform,
                            Player.Player.Self);
                    startTile = vehicle.GetTileLocationAfterAction(actionNode.Previous, out _);
                    targetTile = map.Tiles[action.TargetTileId] as Tile;

                    transform.localPosition = startTile.PositionOnSphere;
                    transform.localRotation = Quaternion.LookRotation(
                        (targetTile.PositionOnSphere - startTile.PositionOnSphere).normalized,
                        transform.localPosition.normalized);
                    break;

                case VehicleAction.ActionType.UnloadTruck:
                    FixedGeometry = GeometriesManager.Instance.GetGameObjectGeometry(
                        GeometriesManager.GeometryType.ActionUnload, EntityId, transform, Player.Player.Self);
                    startTile = map.Tiles[action.TargetTileId] as Tile;

                    transform.localPosition = startTile.PositionOnSphere;
                    transform.localRotation = Quaternion.LookRotation(
                        (startTile.NeighborTiles[0].LeftVertex - startTile.PositionOnSphere).normalized,
                        transform.localPosition.normalized);
                    break;
            }

            FixedGeometry?.SetCustomColor(GeometriesManager.Instance.actionColor);


            map.EntityIdManager[EntityId] = this;
        }

        public void InitPreview(VehicleAction action, Tile startTile)
        {
            GameObject[] children = new GameObject[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                children[i] = transform.GetChild(i).gameObject;
            }

            transform.DetachChildren();

            foreach (GameObject child in children)
            {
                Destroy(child);
            }

            var map = Map.Map.Instance;

            Tile targetTile;


            switch (action.Type)
            {
                case VehicleAction.ActionType.LoadTruck:
                case VehicleAction.ActionType.WaitForTruck:
                    var geometryType = action.Type == VehicleAction.ActionType.LoadTruck
                        ? GeometriesManager.GeometryType.ActionLoad
                        : GeometriesManager.GeometryType.ActionWait;
                    FixedGeometry =
                        GeometriesManager.Instance.GetGameObjectGeometry(geometryType, -1, transform,
                            Player.Player.Self);
                    targetTile = map.Tiles[action.TargetTileId] as Tile;

                    transform.localPosition = startTile.PositionOnSphere;
                    transform.localRotation = Quaternion.LookRotation(
                        (targetTile.PositionOnSphere - startTile.PositionOnSphere).normalized,
                        transform.localPosition.normalized);
                    break;

                case VehicleAction.ActionType.UnloadTruck:
                    FixedGeometry = GeometriesManager.Instance.GetGameObjectGeometry(
                        GeometriesManager.GeometryType.ActionUnload, -1, transform, Player.Player.Self);
                    targetTile = map.Tiles[action.TargetTileId] as Tile;

                    transform.localPosition = targetTile.PositionOnSphere;
                    transform.localRotation = Quaternion.LookRotation(
                        (targetTile.NeighborTiles[0].LeftVertex - targetTile.PositionOnSphere).normalized,
                        transform.localPosition.normalized);
                    break;
            }

            FixedGeometry.SetCustomColor(GeometriesManager.Instance.actionPreviewColor);
            FixedGeometry.CurrentlyHoverable = false;
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
            Constants.OutlineData outlineData;
            switch (hoverState)
            {
                case HoverState.Valid:
                    outlineData = GeometriesManager.Instance.routeQueuedOutline;
                    outlineData.outlineColor.a = 1.0f;
                    outlineData.innerColor.a = 1.0f;
                    break;
                case HoverState.Invalid:
                default:
                    outlineData = GeometriesManager.Instance.invalid;
                    break;
            }

            FixedGeometry?.SetOutlineLayer();
            FixedGeometry?.SetOutlineParameters(outlineData);

            RouteGeometry?.ShowHoverOutline(hoverState);
        }

        public void SetHoverableStatus(bool isHoverable)
        {
            // if (FixedGeometry)
            // {
            //     FixedGeometry.CurrentlyHoverable = isHoverable;
            // }
            // 
            // if (RouteGeometry)
            // {
            //     RouteGeometry.CurrentlyHoverable = isHoverable;
            // }
        }
    }
}
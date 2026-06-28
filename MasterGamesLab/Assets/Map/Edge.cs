using System;
using System.Collections.Generic;
using Map.GeometryGeneration.Edges;
using Map.Hoverables;
using Unity.Netcode;
using UnityEngine;
using Networking;
using IState = Networking.IState;
using Player;
using Map.OutlineEffect;

namespace Map
{
    public class Edge : Timestamped, ISynchableObject<Edge.EdgeState>, IHoverable
    {
        [System.Serializable]
        public enum EdgeType : byte
        {
            None,
            Road,
            Canal
        }

        public enum VisualEdgeState
        {
            None,
            Hovered,
            RouteSelected,
            RouteSuggested,
            RouteCompleted,
        }

        public struct EdgeState : IState, INetworkSerializeByMemcpy
        {
            public EdgeId Id;
            public EdgeType Type;
            public PlayerId OwnerId;

            public int ArrayIndex
            {
                get => Id;
                set => Id = new EdgeId(value);
            }

            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public struct PartialEdgeGeometry
        {
            public List<Vector3> Vertices;
            public List<Vector4> UV1;
            public List<int> Triangles;

            public static PartialEdgeGeometry Empty => new PartialEdgeGeometry
                { Vertices = new List<Vector3>(), UV1 = new List<Vector4>(), Triangles = new List<int>() };
        }

        public readonly EdgeId Id;

        public EntityId EntityId => new(Map.Instance.EntityIdManager.EdgeRange.Start.Value + Id);

        public readonly Tile StartTile;
        public readonly Tile EndTile;

        public new Timestamp Timestamp => base.Timestamp;

        private EdgeType type;

        public EdgeType Type
        {
            get { return type; }
            set
            {
                type = value;
                Touch();
                TriggerGeometryChange();
            }
        }

        private Player.Player owner;

        public Player.Player Owner
        {
            get { return owner; }
            set
            {
                owner = value;
                Touch();
                TriggerDirty();
            }
        }

        private VisualEdgeState visualState;

        public VisualEdgeState VisualState
        {
            get { return visualState; }
            set
            {
                visualState = value;
                TriggerDirty();
            }
        }

        private EdgeType blueprintType;

        public EdgeType BlueprintType
        {
            get { return blueprintType; }
            set
            {
                blueprintType = value;
                TriggerGeometryChange();
            }
        }

        private bool blueprintPreview;

        public bool BlueprintPreview
        {
            get { return blueprintPreview; }
            set
            {
                blueprintPreview = value;
                TriggerDirty();
            }
        }

        private bool blueprintIsValid;

        public bool BlueprintIsValid
        {
            get { return blueprintIsValid; }
            set
            {
                blueprintIsValid = value;
                TriggerDirty();
            }
        }

        public int BlueprintCost = 0;

        public Blueprint.VisualState BlueprintVisualState
        {
            get
            {
                if (blueprintType == EdgeType.None) return Blueprint.VisualState.Valid;
                if (BlueprintPreview)
                    return Type == EdgeType.None
                        ? Blueprint.VisualState.Preview
                        : Blueprint.VisualState.PreviewOverlapping;
                if (type == blueprintType) return Blueprint.VisualState.Overlapping;
                return BlueprintIsValid ? Blueprint.VisualState.Valid : Blueprint.VisualState.Invalid;
            }
        }

        public EdgeState State
        {
            get => new EdgeState { Id = Id, Type = type, OwnerId = owner?.Id ?? PlayerId.NONE };
            set
            {
                Type = value.Type;
                Owner = value.OwnerId != PlayerId.NONE ? Map.Instance.Players[value.OwnerId] : null;
            }
        }

        public bool EdgeDirty;

        public Vector3 VertexA { get; private set; }
        public Vector3 VertexB { get; private set; }
        private EdgeGeometry geometry;
        private EdgeGeometry blueprintGeometry;

        private Constants.OutlineData? outline;

        public Edge(EdgeId id, Tile startTile, Tile endTile, EdgeType type, Player.Player player, Vector3 vertexA,
            Vector3 vertexB)
        {
            Id = id;
            StartTile = startTile;
            EndTile = endTile;
            this.type = type;
            owner = player;
            this.VertexA = vertexA;
            this.VertexB = vertexB;
            outline = null;
            Touch();
        }

        public void ApplyServerState(EdgeState state, double _)
        {
            State = state;
            ResetDirty();
        }

        public bool CanBecomeRoad()
        {
            return Type == EdgeType.None && StartTile.Type != Tile.TileType.Mountain &&
                   StartTile.Type != Tile.TileType.Water && EndTile.Type != Tile.TileType.Mountain &&
                   EndTile.Type != Tile.TileType.Water;
        }

        public bool CanBecomeCanal()
        {
            if (Type != EdgeType.None) return false;
            var startHasWater = StartTile.Type == Tile.TileType.Water ||
                                StartTile.CountEdgesWith(e => e.Type == EdgeType.Canal) > 0;
            var endHasWater = EndTile.Type == Tile.TileType.Water ||
                              EndTile.CountEdgesWith(e => e.Type == EdgeType.Canal) > 0;

            var startCanBuild = StartTile.Type == Tile.TileType.Plain || StartTile.Type == Tile.TileType.Forest;
            var endCanBuild = EndTile.Type == Tile.TileType.Plain || EndTile.Type == Tile.TileType.Forest;
            return (startHasWater && endCanBuild) || (startCanBuild && endHasWater);
        }

        public bool CanBecomeType(EdgeType type)
        {
            switch (type)
            {
                case EdgeType.Road: return CanBecomeRoad();
                case EdgeType.Canal: return CanBecomeCanal();
            }

            return true;
        }

        public bool CanBecomeBlueprintType(EdgeType type)
        {
            if (BlueprintType != EdgeType.None && BlueprintType != type && !BlueprintPreview) return false;
            if (Type == type) return true;

            if (type == EdgeType.Canal)
            {
                if (Type != EdgeType.None) return false;

                //var startHasWater = StartTile.Type == Tile.TileType.Water || StartTile.CountEdgesWith(e => e.Type == EdgeType.Canal || e.BlueprintType == EdgeType.Canal) > 0;
                //var endHasWater = EndTile.Type == Tile.TileType.Water || EndTile.CountEdgesWith(e => e.Type == EdgeType.Canal || e.BlueprintType == EdgeType.Canal) > 0;

                var startCanBuild = StartTile.Type == Tile.TileType.Plain || StartTile.Type == Tile.TileType.Forest;
                var endCanBuild = EndTile.Type == Tile.TileType.Plain || EndTile.Type == Tile.TileType.Forest;
                return (endCanBuild) || (startCanBuild);
            }
            else if (CanBecomeType(type)) return true;

            return false;
        }

        public int GetTraversalCost(Player.Player player)
        {
            return Type switch
            {
                EdgeType.Road =>
                    owner == null ? Constants.ROAD_TRAVERSAL_COST_PUBLIC :
                    owner == player ? Constants.ROAD_TRAVERSAL_COST_OWN :
                    Constants.ROAD_TRAVERSAL_COST_ENEMY,

                EdgeType.Canal =>
                    owner == null ? Constants.CANAL_TRAVERSAL_COST_PUBLIC :
                    owner == player ? Constants.CANAL_TRAVERSAL_COST_OWN :
                    Constants.CANAL_TRAVERSAL_COST_ENEMY,
                _ => 0,
            };
        }

        public float GetSpeedMultiplier()
        {
            return Type switch
            {
                EdgeType.Road => Constants.ROAD_SPEED_MULTIPLIER,
                EdgeType.Canal => Constants.CANAL_SPEED_MULTIPLIER,
                _ => 1.0f,
            };
        }

        public void SetGeometryFrom(PartialEdgeGeometry partialGeometry, Tile sender)
        {
            if (geometry == null)
            {
                geometry = sender.Chunk.RequestNewEdgeGeometry();
                geometry.SetLayerNames("Default", "Outline", "Outline Transparent");
            }

            if (sender.Id == StartTile.Id)
            {
                geometry.SetStartMesh(partialGeometry);
            }
            else
            {
                geometry.SetEndMesh(partialGeometry);
            }

            SetColorAndOutline();
            // geometry.SetLayer(EdgeGeometry.outlineLayer);
            // geometry.SetRoadColor(Constants.ROAD_BLUEPRINT_INVALID_COLOR);
            // geometry.SetOutlineParameters(Constants.ROAD_BLUEPRINT_INVALID_OUTLINE);
        }

        public void SetOutlineParameters(Constants.OutlineData outlineData, bool transparent)
        {
            if (transparent)
            {
                geometry.SetOutlineTransparentLayer();
                blueprintGeometry.SetOutlineTransparentLayer();
            }
            else
            {
                geometry.SetOutlineLayer();
                blueprintGeometry.SetOutlineLayer();
            }

            geometry.SetOutlineParameters(outlineData);
            blueprintGeometry.SetOutlineParameters(outlineData);
        }

        public void SetBluePrintGeometryFrom(PartialEdgeGeometry partialGeometry, Tile sender)
        {
            if (blueprintGeometry == null)
            {
                blueprintGeometry = sender.Chunk.RequestNewEdgeGeometry();
            }

            if (sender.Id == StartTile.Id)
            {
                blueprintGeometry.SetStartMesh(partialGeometry);
            }
            else
            {
                blueprintGeometry.SetEndMesh(partialGeometry);
            }

            SetBlueprintColorAndOutline();
        }

        public void ChangeVisualState()
        {
            SetBlueprintColorAndOutline();
            SetColorAndOutline();
            if (outline is { } o)
            {
                SetOutlineParameters(o, Type == EdgeType.Canal);
            }

            EdgeDirty = false;
        }

        private void SetColorAndOutline()
        {
            if (Owner != null)
            {
                geometry.SetPlayerColor(Owner.Color);
            }
            else
            {
                geometry.SetPlayerColor(Color.black);
            }

            geometry.SetBaseLayer();
        }

        private void SetBlueprintColorAndOutline()
        {
            switch (BlueprintVisualState)
            {
                case Blueprint.VisualState.Valid:

                    switch (BlueprintType)
                    {
                        case EdgeType.Road:
                            blueprintGeometry.SetOutlineLayer();
                            blueprintGeometry.SetPlayerColor(Constants.ROAD_BLUEPRINT_COLOR);
                            blueprintGeometry.SetOutlineParameters(Constants.ROAD_BLUEPRINT_VALID_OUTLINE);
                            break;
                        case EdgeType.Canal:
                            blueprintGeometry.SetOutlineTransparentLayer();
                            blueprintGeometry.SetOutlineParameters(Constants.CANAL_BLUEPRINT_VALID_OUTLINE);
                            break;
                        default:
                            blueprintGeometry.SetBaseLayer();
                            break;
                    }

                    break;
                case Blueprint.VisualState.Preview:
                    blueprintGeometry.SetBaseLayer();
                    blueprintGeometry.SetPlayerColor(Constants.ROAD_BLUEPRINT_PREVIEW_COLOR);
                    break;
                case Blueprint.VisualState.PreviewOverlapping:
                    blueprintGeometry.SetOutlineTransparentLayer();
                    blueprintGeometry.SetOutlineParameters(Constants.ROAD_BLUEPRINT_PREVIEW_OVERLAPPING_OUTLINE);
                    break;
                case Blueprint.VisualState.Overlapping:
                    blueprintGeometry.SetOutlineTransparentLayer();
                    blueprintGeometry.SetOutlineParameters(Constants.ROAD_BLUEPRINT_OVERLAPPING_OUTLINE);
                    break;
                case Blueprint.VisualState.Invalid:
                    blueprintGeometry.SetOutlineLayer();
                    blueprintGeometry.SetPlayerColor(Constants.ROAD_BLUEPRINT_INVALID_COLOR);
                    blueprintGeometry.SetOutlineParameters(Constants.ROAD_BLUEPRINT_INVALID_OUTLINE);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void TriggerGeometryChange()
        {
            StartTile.GeometryChanged = true;
            EndTile.GeometryChanged = true;
        }

        public void TriggerDirty()
        {
            EdgeDirty = true;
            // StartTile.EdgeDirty = true;
            // EndTile.EdgeDirty = true;
        }

        public Vector4 GetEdgeData()
        {
            //return new Vector4(Id + Map.ID_OFFSET, randomValue, active ? 1 : 0, 0);
            return new Vector4(EntityId.Value + Map.ID_OFFSET, 0, 0, 0);
        }


        public void ClearOutline()
        {
            outline = null;
            TriggerDirty();
        }

        public void ShowOutline(Constants.OutlineData outlineData)
        {
            outline = outlineData;
            TriggerDirty();
        }

        public void ShowHoverOutline(HoverState hoverState = HoverState.Valid)
        {
            var outlineData = hoverState switch
            {
                HoverState.Invalid => Constants.ROAD_BLUEPRINT_INVALID_OUTLINE,
                _ => Constants.HOVER_OUTLINE_FILLED_IN,
            };
            ShowOutline(outlineData);
        }
    }
}
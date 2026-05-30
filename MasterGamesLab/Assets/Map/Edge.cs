using System;
using System.Collections.Generic;
using Map.GeometryGeneration.Edges;
using Unity.Netcode;
using UnityEngine;

namespace Map
{
    public class Edge : Timestamped, ISynchableObject<Edge.EdgeState>
    {
        [System.Serializable]
        public enum EdgeType : byte
        {
            None,
            Road,
            Canal,
            Rail
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
            public PlayerId Owner;

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
            public List<int> Triangles;

            public static PartialEdgeGeometry Empty => new PartialEdgeGeometry
                { Vertices = new List<Vector3>(), Triangles = new List<int>() };
        }

        public readonly EdgeId Id;

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

        private PlayerId owner;

        public PlayerId Owner
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

        public bool BlueprintPreview;

        public Blueprint.VisualState BlueprintVisualState
        {
            get
            {
                if (blueprintType == EdgeType.None) return Blueprint.VisualState.Valid;
                if (BlueprintPreview) return Blueprint.VisualState.Preview;
                if (type == EdgeType.None) return Blueprint.VisualState.Valid;
                if (type == blueprintType) return Blueprint.VisualState.Overlapping;
                return Blueprint.VisualState.Invalid;
            }
        }

        public EdgeState State
        {
            get => new EdgeState { Id = Id, Type = type, Owner = owner };
            set
            {
                Type = value.Type;
                Owner = value.Owner;
            }
        }

        public bool EdgeDirty;

        public Vector3 VertexA { get; private set; }
        public Vector3 VertexB { get; private set; }
        private EdgeGeometry geometry;
        private EdgeGeometry blueprintGeometry;

        public Edge(EdgeId id, Tile startTile, Tile endTile, EdgeType type, PlayerId playerId, Vector3 vertexA,
            Vector3 vertexB)
        {
            Id = id;
            StartTile = startTile;
            EndTile = endTile;
            this.type = type;
            owner = playerId;
            this.VertexA = vertexA;
            this.VertexB = vertexB;
            Touch();
        }

        public void ApplyServerState(EdgeState state, double _)
        {
            State = state;
            ResetDirty();
        }

        public bool CanBecomeRoad(bool blueprint = false)
        {
            return (blueprint ? BlueprintType : Type) == EdgeType.None && StartTile.Type != Tile.TileType.Mountain &&
                   StartTile.Type != Tile.TileType.Water && EndTile.Type != Tile.TileType.Mountain &&
                   EndTile.Type != Tile.TileType.Water;
        }

        public bool CanBecomeCanal(bool blueprint = false)
        {
            if ((blueprint ? BlueprintType : Type) != EdgeType.None) return false;
            var startHasWater = StartTile.Type == Tile.TileType.Water ||
                                StartTile.CountEdgesWithType(EdgeType.Canal, blueprint) > 0;
            var endHasWater = EndTile.Type == Tile.TileType.Water ||
                              EndTile.CountEdgesWithType(EdgeType.Canal, blueprint) > 0;

            var startCanBuild = StartTile.Type == Tile.TileType.Plain || StartTile.Type == Tile.TileType.Forest;
            var endCanBuild = EndTile.Type == Tile.TileType.Plain || EndTile.Type == Tile.TileType.Forest;
            return (startHasWater && endCanBuild) || (startCanBuild && endHasWater);
        }

        public bool CanBecomeRail(bool blueprint = false)
        {
            // TODO correct rail condition
            return false;
        }

        public bool CanBecomeType(EdgeType type, bool blueprint = false)
        {
            switch (type)
            {
                case EdgeType.Road: return CanBecomeRoad(blueprint);
                case EdgeType.Canal: return CanBecomeCanal(blueprint);
                case EdgeType.Rail: return CanBecomeRail(blueprint);
            }

            return true;
        }

        public void SetGeometryFrom(PartialEdgeGeometry partialGeometry, Tile sender)
        {
            if (geometry == null)
            {
                geometry = sender.Chunk.RequestNewEdgeGeometry();
            }

            if (sender.Id == StartTile.Id)
            {
                geometry.SetStartMesh(partialGeometry);
            }
            else
            {
                geometry.SetEndMesh(partialGeometry);
            }

            geometry.SetRoadColor(PlayerManager.Instance.GetPlayerColor(Owner));

            if (Type == EdgeType.Canal)
            {
                geometry.SetRoadColor(new Color(0, 0, 255, 1));
                geometry.SetLayer(EdgeGeometry.outlineLayer);
                geometry.SetOutlineParameters(Constants.ROAD_BLUEPRINT_OVERLAPPING_OUTLINE);
            }

            // geometry.SetLayer(EdgeGeometry.outlineLayer);
            // geometry.SetRoadColor(Constants.ROAD_BLUEPRINT_INVALID_COLOR);
            // geometry.SetOutlineParameters(Constants.ROAD_BLUEPRINT_INVALID_OUTLINE);
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

            switch (BlueprintVisualState)
            {
                case Blueprint.VisualState.Valid:
                    blueprintGeometry.SetLayer(EdgeGeometry.defaultLayer);
                    blueprintGeometry.SetRoadColor(Constants.ROAD_BLUEPRINT_COLOR);
                    break;
                case Blueprint.VisualState.Preview:
                    blueprintGeometry.SetLayer(EdgeGeometry.defaultLayer);
                    blueprintGeometry.SetRoadColor(Constants.ROAD_BLUEPRINT_PREVIEW_COLOR);
                    break;
                case Blueprint.VisualState.Overlapping:
                    blueprintGeometry.SetLayer(EdgeGeometry.outlineTransparentLayer);
                    blueprintGeometry.SetOutlineParameters(Constants.ROAD_BLUEPRINT_OVERLAPPING_OUTLINE);
                    break;
                case Blueprint.VisualState.Invalid:
                    blueprintGeometry.SetLayer(EdgeGeometry.outlineLayer);
                    blueprintGeometry.SetRoadColor(Constants.ROAD_BLUEPRINT_INVALID_COLOR);
                    blueprintGeometry.SetOutlineParameters(Constants.ROAD_BLUEPRINT_INVALID_OUTLINE);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ChangeVisualState()
        {
            EdgeDirty = false;
        }

        private void TriggerGeometryChange()
        {
            StartTile.GeometryChanged = true;
            EndTile.GeometryChanged = true;
        }

        private void TriggerDirty()
        {
            EdgeDirty = true;
            // StartTile.EdgeDirty = true;
            // EndTile.EdgeDirty = true;
        }
    }
}
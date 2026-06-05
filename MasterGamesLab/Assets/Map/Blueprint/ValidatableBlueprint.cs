using Map.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Map.Blueprint
{
    public abstract class ValidatableBlueprint
    {
        protected abstract IEnumerable<Edge> EnumerateEdges();
        protected abstract IEnumerable<Structure> EnumerateStructures();

        protected abstract void SetValid(Edge edge, bool valid, int cost = 0);
        protected abstract bool IsValid(Edge edge);
        protected abstract int Cost(Edge edge);
        protected abstract Edge.EdgeType BlueprintedEdgeType(Edge edge);

        protected Edge.EdgeType ConfirmedEdgeType(Edge edge)
        {
            if (edge.Type != Edge.EdgeType.None) return edge.Type;
            if (IsValid(edge)) return BlueprintedEdgeType(edge);
            return Edge.EdgeType.None;

        }

        protected abstract void SetValid(Structure structure, bool valid, int cost);
        protected abstract bool IsValid(Structure structure);
        protected abstract int Cost(Structure structure);
        protected abstract Structure BlueprintedStructure(Tile tile);

        protected Structure ConfirmedStructure(Tile tile)
        {
            if (tile.Structure != null) return tile.Structure;
            if (IsValid(BlueprintedStructure(tile))) return BlueprintedStructure(tile);
            return null;

        }

        public void Validate()
        {
            foreach (var edge in EnumerateEdges()) SetValid(edge, false, 0);
            foreach (var structure in EnumerateStructures()) SetValid(structure, false, 0);

            foreach (var edge in EnumerateEdges()) ValidateEdge(edge);
            foreach (var structure in EnumerateStructures()) ValidateStructure(structure);
        }

        public bool ValidateEdge(Edge edge)
        {
            if(IsValid(edge)) return true;

            switch(BlueprintedEdgeType(edge))
            {
                case Edge.EdgeType.Road:
                    if(edge.Type != Edge.EdgeType.None) return false;
                    if(edge.StartTile.CanBuild(out float factor1) && edge.EndTile.CanBuild(out float factor2))
                    {
                        var factor = (factor1 + factor2) / 2;
                        SetValid(edge, true, (int)Math.Round(factor * Constants.ROAD_BUILD_COST));
                        return true;
                    }
                    return false;
                case Edge.EdgeType.Canal:
                    return ValidateCanalRecursive(edge);
            }
            return false;
        }

        private bool ValidateCanalRecursive(Edge edge)
        {
            if(BlueprintedEdgeType(edge) != Edge.EdgeType.Canal) return false;

            if(edge.Type != Edge.EdgeType.None || edge.StartTile.Structure != null || edge.EndTile.Structure != null) 
                return false;


            bool startCanBuild = edge.StartTile.CanBuild(out float factor1) || edge.StartTile.Type == Tile.TileType.Water;
            bool endCanBuild = edge.EndTile.CanBuild(out float factor2) || edge.EndTile.Type == Tile.TileType.Water;


            if(!(startCanBuild && endCanBuild)) return false;

            float factor = (factor1 + factor2) / 2;

            SetValid(edge, true, (int)Math.Round(factor * Constants.BASE_CANAL_BUILD_COST));
            return true;
        }

        public bool ValidateStructure(Structure structure)
        {
            if(structure == null) return false;
            if (IsValid(structure)) return true;

            switch (structure.Type)
            {
                case Structure.StructureType.Port:
                    if (structure.Tile != null || structure.BlueprintTile?.BlueprintStructure != structure) return false;

                    Tile tile = structure.BlueprintTile;
                    if (tile.Structure != null) return false;

                    float factor;
                    if(!tile.CanBuild(out factor)) return false;

                    if (tile.Neighbors.Where(tile => tile.Type == Tile.TileType.Water).Count() == 0) return false;

                    if (tile.CountEdgesWith(edge => !(ConfirmedEdgeType(edge) is Edge.EdgeType.None or Edge.EdgeType.Road)) > 0) return false;

                    SetValid(structure, true, (int)(factor * Constants.PORT_BUILD_COST));
                    return true;
            }
            return false;
        }
    }
}
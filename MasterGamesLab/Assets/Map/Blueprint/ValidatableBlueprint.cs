using Map.Infrastructure;
using System;
using System.Collections.Generic;

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
            if (IsValid(edge)) BlueprintedEdgeType(edge);
            return Edge.EdgeType.None;

        }

        protected abstract void SetValid(Structure structure, bool valid, int cost);
        protected abstract bool IsValid(Structure structure);
        protected abstract int Cost(Structure structure);
        protected abstract Structure BlueprintedStructure(Tile tile);

        protected Structure ConfirmedStructure(Tile tile)
        {
            if (tile.Structure != null) return tile.Structure;
            if (IsValid(BlueprintedStructure(tile))) BlueprintedStructure(tile);
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
                    SetValid(edge, true, Constants.BASE_CANAL_BUILD_COST);
                    return true;
            }
            return false;
        }

        public bool ValidateStructure(Structure structure)
        {
            if (IsValid(structure)) return true;

            switch (structure.Type)
            {
                case Structure.StructureType.Port:
                    SetValid(structure, true, Constants.PORT_BUILD_COST);
                    return true;
            }
            return false;
        }
    }
}
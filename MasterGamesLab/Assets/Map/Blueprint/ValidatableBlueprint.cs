using Map.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Transactions;

namespace Map.Blueprint
{
    public abstract class ValidatableBlueprint
    {
        private SortedList<EdgeId, int> canalDepths;
        private Queue<Edge> canalQueue;

        protected abstract IEnumerable<Edge> EnumerateEdges();
        protected abstract IEnumerable<Structure> EnumerateStructures();

        protected abstract void SetValid(Edge edge, bool valid, int cost);
        public abstract bool IsValid(Edge edge);
        public abstract int Cost(Edge edge);
        public abstract Edge.EdgeType BlueprintedEdgeType(Edge edge);

        protected Edge.EdgeType ConfirmedEdgeType(Edge edge)
        {
            if (edge.Type != Edge.EdgeType.None) return edge.Type;
            if (IsValid(edge)) return BlueprintedEdgeType(edge);
            return Edge.EdgeType.None;

        }

        protected abstract void SetValid(Structure structure, bool valid, int cost);
        public abstract bool IsValid(Structure structure);
        public abstract int Cost(Structure structure);
        public abstract StructureId BlueprintedStructure(Tile tile);
        public abstract Tile BlueprintedStructureTile(Structure structure);

        protected StructureId ConfirmedStructure(Tile tile)
        {
            if (tile.Structure != null) return tile.Structure.Id;
            if (IsValid(Map.Instance.Infrastructure[BlueprintedStructure(tile)])) return BlueprintedStructure(tile);
            return StructureId.NONE;

        }

        public void Validate()
        {
            canalDepths = new();
            canalQueue = new();

            foreach (var edge in EnumerateEdges()) SetValid(edge, false, 0);
            foreach (var structure in EnumerateStructures()) SetValid(structure, false, 0);

            foreach (var edge in EnumerateEdges()) ValidateEdge(edge);
            ValidateCanals();
            foreach (var structure in EnumerateStructures()) ValidateStructure(structure);
        }

        public void ValidateEdge(Edge edge)
        {
            if(IsValid(edge)) return;

            switch(BlueprintedEdgeType(edge))
            {
                case Edge.EdgeType.Road:
                    if(edge.Type != Edge.EdgeType.None) return;
                    if(edge.StartTile.CanBuild(out float factor1) && edge.EndTile.CanBuild(out float factor2))
                    {
                        var factor = (factor1 + factor2) / 2;
                        SetValid(edge, true, (int)Math.Round(factor * Constants.ROAD_BUILD_COST));
                    }
                    return;
                case Edge.EdgeType.Canal:
                    ValidateCanalCandidate(edge);
                    return;
            }
        }

        private void ValidateCanals()
        {
            while(canalQueue.Count > 0)
            {
                var edge = canalQueue.Dequeue();
                var nextDepth = canalDepths[edge.Id] + 1;

                foreach(var e in edge.StartTile.Edges.Concat(edge.EndTile.Edges))
                {
                    bool isContained = canalDepths.ContainsKey(e.Id);
                    if ((isContained && (canalDepths[e.Id] > nextDepth || canalDepths[e.Id] == -1)) || (!isContained && e.Type == Edge.EdgeType.Canal))
                    {
                        canalDepths[e.Id] = nextDepth;
                        canalQueue.Enqueue(e);
                    }
                }
            }

            foreach(var edge in EnumerateEdges())
            {
                if(canalDepths.ContainsKey(edge.Id))
                {
                    var depth = canalDepths[edge.Id];

                    if(depth != -1)
                    {
                        SetValid(edge, true, depth);
                    }
                }
            }
        }

        private void ValidateCanalCandidate(Edge edge)
        {
            if(BlueprintedEdgeType(edge) != Edge.EdgeType.Canal) return;

            if(edge.Type != Edge.EdgeType.None || edge.StartTile.Structure != null || edge.EndTile.Structure != null) 
                return;


            bool startCanBuild = edge.StartTile.CanBuild(out _) || edge.StartTile.Type == Tile.TileType.Water;
            bool endCanBuild = edge.EndTile.CanBuild(out _) || edge.EndTile.Type == Tile.TileType.Water;

            bool startIsWater = edge.StartTile.Type == Tile.TileType.Water;
            bool endIsWater = edge.EndTile.Type == Tile.TileType.Water;


            if (!((startCanBuild && endCanBuild) || (startCanBuild && endIsWater) || (startIsWater && endCanBuild))) return;

            //float factor = (factor1 + factor2) / 2;


            if (startIsWater || endIsWater)
            {
                canalQueue.Enqueue(edge);
                canalDepths.Add(edge.Id, 0);
            }
            else
                canalDepths.Add(edge.Id, -1);
        }

        public bool ValidateStructure(Structure structure)
        {
            if(structure == null) return false;
            if (IsValid(structure)) return true;

            switch (structure.Type)
            {
                case Structure.StructureType.Port:
                    Tile tile = BlueprintedStructureTile(structure);
                    if (structure.Tile != null || ConfirmedStructure(tile) != StructureId.NONE) return false;

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
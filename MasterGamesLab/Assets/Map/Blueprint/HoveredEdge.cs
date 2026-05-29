

using System.Collections.Generic;

namespace Map.Blueprint
{
    public struct HoveredEdge
    {
        public Tile StartTile;
        public Tile EndTile;
        public Edge.EdgeType Type;
        public VisualState VisualState;
    }

    public struct HoveredEdges : IHoveredObject
    {
        public List<HoveredEdge> Edges;
    }
}
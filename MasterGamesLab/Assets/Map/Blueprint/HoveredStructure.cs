using Map.Infrastructure;

namespace Map.Blueprint
{
    public struct HoveredStructure
    {
        public Tile Tile;
        public Structure.StructureType Type;
        public VisualState VisualState;
    }
}
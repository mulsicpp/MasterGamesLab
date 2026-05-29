using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

namespace Map.Blueprint
{
    public class Blueprint
    {
        private List<EdgeId> edgeIds;
        public IReadOnlyList<EdgeId> EdgeIds => edgeIds;

        public Blueprint()
        {
            edgeIds = new List<EdgeId>();
        }

        public bool AddEdge(EdgeId id, Edge.EdgeType type)
        {
            if (edgeIds.Contains(id)) return false;
            Map.Instance.Edges[id].BlueprintType = type;
            edgeIds.Add(id);
            return true;
        }

        public void RemoveEdge(EdgeId id)
        {
            Map.Instance.Edges[id].BlueprintType = Edge.EdgeType.None;
            if (edgeIds.Contains(id)) edgeIds.Remove(id);
        }

        public void Clear()
        {
            foreach(var id in edgeIds)
                Map.Instance.Edges[id].BlueprintType = Edge.EdgeType.None;
            edgeIds.Clear();
        }
    }
}
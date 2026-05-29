using Map.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

namespace Map.Blueprint
{
    public class Blueprint
    {
        private List<EdgeId> edgeIds;
        public IReadOnlyList<EdgeId> EdgeIds => edgeIds;

        private List<EdgeId> previewEdgeIds;
        public IReadOnlyList<EdgeId> PreviewEdgeIds => previewEdgeIds;

        private List<TileId> structureTileIds;
        public IReadOnlyList<TileId> StructureTileIds => structureTileIds;

        private TileId previewStructureTileId;
        public TileId PreviewStructureTileId => previewStructureTileId;

        public Blueprint()
        {
            edgeIds = new List<EdgeId>();
            previewEdgeIds = new List<EdgeId>();

            structureTileIds = new List<TileId>();
            previewStructureTileId = TileId.NONE;
        }

        public bool AddEdge(EdgeId edgeId, Edge.EdgeType type)
        {
            if (edgeIds.Contains(edgeId)) return false;
            var edge = Map.Instance.Edges[edgeId];
            edge.BlueprintType = type;
            edge.BlueprintPreview = false;
            edgeIds.Add(edgeId);
            return true;
        }

        public void RemoveEdge(EdgeId edgeId)
        {
            var edge = Map.Instance.Edges[edgeId];
            edge.BlueprintType = Edge.EdgeType.None;
            edge.BlueprintPreview = false;
            if (edgeIds.Contains(edgeId)) edgeIds.Remove(edgeId);
        }

        public bool AddStructure(TileId tileId, Structure.StructureType type)
        {
            if (structureTileIds.Contains(tileId)) return false;
            ((Tile)Map.Instance.Tiles[tileId]).BlueprintStructureType = type;
            structureTileIds.Add(tileId);
            return true;
        }

        public void RemoveStructure(TileId tileId)
        {
            ((Tile)Map.Instance.Tiles[tileId]).BlueprintStructureType = null;
            if (structureTileIds.Contains(tileId)) structureTileIds.Remove(tileId);
        }

        public void SetPreviewEdges(TileId[] pathIds, Edge.EdgeType type)
        {
            foreach (var edgeId in previewEdgeIds)
            {
                var edge = Map.Instance.Edges[edgeId];
                edge.BlueprintType = Edge.EdgeType.None;
                edge.BlueprintPreview = false;
            }
            previewEdgeIds.Clear();

            if (pathIds == null) return;

            var path = pathIds.Select(id => Map.Instance.Tiles[id]).ToArray();

            for (int i = 1; i < path.Length; i++)
            {
                var edge = path[i - 1].FindEdgeTo(path[i]);
                if (edge != null && !edgeIds.Contains(edge.Id) && edge.CanBecomeRoad())
                {
                    edge.BlueprintType = type;
                    edge.BlueprintPreview = true;
                    previewEdgeIds.Add(edge.Id);
                }
            }
        }

        public void SetPreviewStructure(TileId tileId, Structure.StructureType type)
        {
            if (structureTileIds.Contains(tileId)) return;

            if(previewStructureTileId != TileId.NONE)
            {
                var oldTile = (Tile)Map.Instance.Tiles[previewStructureTileId];
                oldTile.BlueprintStructureType = null;
                oldTile.BlueprintPreview = false;
            }

            if (tileId == TileId.NONE) return;

            var tile = (Tile)Map.Instance.Tiles[previewStructureTileId];
            tile.BlueprintStructureType = type;
            tile.BlueprintPreview = true;
        }

        public void Clear()
        {
            foreach(var id in edgeIds)
                Map.Instance.Edges[id].BlueprintType = Edge.EdgeType.None;
            edgeIds.Clear();
        }

        public void ApplyPreviewEdges()
        {
            foreach(var edgeId in previewEdgeIds)
            {
                var edge = Map.Instance.Edges[edgeId];
                edge.BlueprintPreview = false;
            }
            edgeIds.AddRange(previewEdgeIds);
            previewEdgeIds.Clear();
        }

        public void ApplyPreviewStructure()
        {
            if(previewStructureTileId == TileId.NONE) return;

            var tile = (Tile)Map.Instance.Tiles[previewStructureTileId];
            tile.BlueprintPreview = false;

            structureTileIds.Add(previewStructureTileId);
            previewStructureTileId = TileId.NONE;
        }
    }
}
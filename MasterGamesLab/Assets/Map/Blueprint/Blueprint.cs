using Blueprint;
using Map.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            Tile tile = (Tile)Map.Instance.Tiles[tileId];
            tile.BlueprintStructureType = type;
            tile.BlueprintPreview = false;
            structureTileIds.Add(tileId);
            return true;
        }

        public void RemoveStructure(TileId tileId)
        {
            Tile tile = (Tile)Map.Instance.Tiles[tileId];
            tile.BlueprintStructureType = null;
            tile.BlueprintPreview = false;
            if (structureTileIds.Contains(tileId)) structureTileIds.Remove(tileId);
        }

        public void ClearPreviewEdges()
        {
            foreach (var edgeId in previewEdgeIds)
            {
                var edge = Map.Instance.Edges[edgeId];
                edge.BlueprintType = Edge.EdgeType.None;
                edge.BlueprintPreview = false;
            }
            previewEdgeIds.Clear();
        }

        public void ClearPreviewStructure()
        {
            if (previewStructureTileId != TileId.NONE)
            {
                var oldTile = (Tile)Map.Instance.Tiles[previewStructureTileId];
                oldTile.BlueprintStructureType = null;
                oldTile.BlueprintPreview = false;

                previewStructureTileId = TileId.NONE;
            }
        }

        public void SetPreviewEdges(TileId[] pathIds, Edge.EdgeType type)
        {
            ClearPreviewEdges();
            ClearPreviewStructure();

            if (pathIds == null) return;

            var path = pathIds.Select(id => Map.Instance.Tiles[id]).ToArray();

            for (int i = 1; i < path.Length; i++)
            {
                var edge = path[i - 1].FindEdgeTo(path[i]);
                if (edge != null && !edgeIds.Contains(edge.Id) && edge.CanBecomeBlueprintType(type))
                {
                    edge.BlueprintType = type;
                    edge.BlueprintPreview = true;
                    previewEdgeIds.Add(edge.Id);
                }
            }
        }

        public void SetPreviewStructure(TileId tileId, Structure.StructureType type)
        {

            ClearPreviewEdges();
            ClearPreviewStructure();

            if (tileId == TileId.NONE) return;

            var tile = (Tile)Map.Instance.Tiles[tileId];

            if (tile.BlueprintStructureType != null || !tile.CanSpawnStructure(type)) return;
            tile.BlueprintStructureType = type;
            tile.BlueprintPreview = true;

            previewStructureTileId = tileId;
        }

        public void Clear()
        {
            foreach(var edgeId in edgeIds)
            {
                var edge = Map.Instance.Edges[edgeId];
                edge.BlueprintType = Edge.EdgeType.None;
                edge.BlueprintPreview = false;
            }
            edgeIds.Clear();

            foreach (var tileId in structureTileIds)
            {
                var tile = (Tile)Map.Instance.Tiles[tileId];
                tile.BlueprintStructureType = null;
                tile.BlueprintPreview = false;
            }
            structureTileIds.Clear();

            ClearPreviewEdges();
            ClearPreviewStructure();
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

        public void ApplyPreview()
        {
            ApplyPreviewEdges();
            ApplyPreviewStructure();
        }

        public void Submit()
        {
            ClearPreviewEdges();
            ClearPreviewStructure();

            List<BlueprintPacket> packets = new();
            BlueprintPacket lastPacket = new();

            foreach(var edgeId in edgeIds)
            {
                lastPacket = lastPacket.AddEdgeToPackets(edgeId, packets);
            }

            Debug.Log("Structure count: " + structureTileIds.Count);

            foreach (var tileId in structureTileIds)
            {
                lastPacket = lastPacket.AddStructureToPackets(tileId, packets);
            }

            if(lastPacket.NettoSize == 0)
            {
                if (packets.Count == 0)
                    return;

                lastPacket = packets.Last();
                packets.RemoveAt(packets.Count - 1);
            }

            foreach(var packet in packets)
            {
                packet.Send(true);
            }
            lastPacket.Send(false);
        }
    }
}
using Map.Fleet;
using Map.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Map.Blueprint
{
    public class Blueprint : ValidatableBlueprint
    {
        private List<Edge> edges;
        public IReadOnlyList<Edge> Edges => edges;

        private List<Edge> previewEdges;
        public IReadOnlyList<Edge> PreviewEdges => previewEdges;

        private List<Structure> structures;
        public IReadOnlyList<Structure> Structures => structures;

        private Structure previewStructure;
        public Structure PreviewStructure => previewStructure;

        private List<Vehicle> vehicles;
        public IReadOnlyList<Vehicle> Vehicles => vehicles;

        private Vehicle previewVehicle;
        public Vehicle PreviewVehicle => previewVehicle;

        public bool IsEmpty => structures.Count == 0 && edges.Count == 0;

        public Blueprint()
        {
            edges = new();
            previewEdges = new();

            structures = new();
            previewStructure = null;

            vehicles = new();
            previewVehicle = null;
        }

        public bool AddEdge(Edge edge, Edge.EdgeType type)
        {
            if (edges.Contains(edge)) return false;
            edge.BlueprintType = type;
            edge.BlueprintPreview = false;
            edges.Add(edge);

            Validate();
            return true;
        }

        public void RemoveEdge(Edge edge)
        {
            edge.BlueprintType = Edge.EdgeType.None;
            edge.BlueprintPreview = false;
            if (edges.Contains(edge)) edges.Remove(edge);
            Validate();
        }

        public bool AddStructure(Tile tile, Structure structure)
        {
            if (structures.Contains(structure)) return false;
            structure.BlueprintTile = tile;
            structure.BlueprintPreview = false;
            structures.Add(structure);

            Validate();
            return true;
        }

        public void RemoveStructure(Structure structure)
        {
            structure.BlueprintTile = null;
            structure.BlueprintPreview = false;
            if (structures.Contains(structure)) structures.Remove(structure);
            Validate();
        }

        public void ClearPreview()
        {
            foreach (var edge in previewEdges)
            {
                edge.BlueprintType = Edge.EdgeType.None;
                edge.BlueprintPreview = false;
            }
            previewEdges.Clear();

            if (previewStructure != null)
            {
                previewStructure.BlueprintTile = null;
                previewStructure.BlueprintPreview = false;

                previewStructure = null;
            }
        }

        // Returns true if a preview could be set from the start tile to the end tile.
        public bool SetPreviewEdges(Tile start, Tile end, Edge.EdgeType type)
        {
            ClearPreview();

            if (start == null || end == null) return false;

            var pathIds = type switch
            {
                Edge.EdgeType.Road => Pathfinding.FindPath(start, end, MovementProfileRegistry.FindRoadBuildPath),
                Edge.EdgeType.Canal => Pathfinding.FindPath(start, end, MovementProfileRegistry.FindCanalBuildPath),
                _ => null
            };

            if (pathIds == null) return false;

            var path = pathIds.Select(id => Map.Instance.Tiles[id]).ToArray();

            for (int i = 1; i < path.Length; i++)
            {
                var edge = path[i - 1].FindEdgeTo(path[i]);
                if (edge != null && !edges.Contains(edge))
                {
                    edge.BlueprintType = type;
                    edge.BlueprintPreview = true;
                    previewEdges.Add(edge);
                }
            }

            return true;
        }

        public bool SetPreviewStructure(Tile tile, Structure.StructureType type)
        {
            ClearPreview();

            if (tile == null) return false;

            var structure = Map.Instance.Infrastructure.GetFirstWith(type, s => !s.Exists && s.BlueprintTile == null && s.Owner == PlayerManager.Instance.SelfId);

            if (structure == null) return false;

            if (tile.BlueprintStructure != null || !tile.CanSpawnStructure(structure.Type)) return false;
            structure.BlueprintTile = tile;
            structure.BlueprintPreview = true;

            previewStructure = structure;

            return true;
        }

        public void Clear()
        {
            foreach (var edge in edges)
            {
                edge.BlueprintType = Edge.EdgeType.None;
                edge.BlueprintPreview = false;
            }
            edges.Clear();

            foreach (var structure in structures)
            {
                structure.BlueprintTile = null;
                structure.BlueprintPreview = false;
            }
            structures.Clear();

            ClearPreview();
        }

        public void ApplyPreviewEdges()
        {
            foreach (var edge in previewEdges)
            {
                edge.BlueprintPreview = false;
            }
            edges.AddRange(previewEdges);
            previewEdges.Clear();

            Validate();
        }

        public void ApplyPreviewStructure()
        {
            if (previewStructure == null) return;

            previewStructure.BlueprintPreview = false;

            structures.Add(previewStructure);
            previewStructure = null;

            Validate();
        }

        public void ApplyPreview()
        {
            ApplyPreviewEdges();
            ApplyPreviewStructure();
        }

        public void Submit()
        {
            ClearPreview();

            List<BlueprintPacket> packets = new();
            BlueprintPacket lastPacket = new();

            foreach (var edge in edges)
            {
                lastPacket = lastPacket.AddEdgeToPackets(edge, packets);
            }

            Debug.Log("Structure count: " + structures.Count);
            
            foreach (var structure in structures)
            {
                lastPacket = lastPacket.AddStructureToPackets(structure, packets);
            }

            if (lastPacket.NettoSize == 0)
            {
                if (packets.Count == 0)
                    return;

                lastPacket = packets.Last();
                packets.RemoveAt(packets.Count - 1);
            }

            foreach (var packet in packets)
            {
                packet.Send(true);
            }
            lastPacket.Send(false);
        }

        protected override IEnumerable<Edge> EnumerateEdges() => edges.AsEnumerable();
        protected override IEnumerable<Structure> EnumerateStructures() => structures.AsEnumerable();

        protected override void SetValid(Edge edge, bool valid, int cost)
        {
            edge.BlueprintIsValid = valid;
            edge.BlueprintCost = cost;
        }

        public override bool IsValid(Edge edge) => edge.BlueprintIsValid;
        public override int Cost(Edge edge) => edge.BlueprintCost;
        public override Edge.EdgeType BlueprintedEdgeType(Edge edge) => !edge.BlueprintPreview ? edge.BlueprintType : Edge.EdgeType.None;


        protected override void SetValid(Structure structure, bool valid, int cost)
        {
            structure.BlueprintIsValid = valid;
            structure.BlueprintCost = cost;
        }

        public override bool IsValid(Structure structure) => structure.BlueprintIsValid;
        public override int Cost(Structure structure) => structure.BlueprintCost;
        public override StructureId BlueprintedStructure(Tile tile) => (!tile.BlueprintStructure?.BlueprintPreview ?? false) ? tile.BlueprintStructure.Id : StructureId.NONE;
        public override Tile BlueprintedStructureTile(Structure structure) => structure.BlueprintTile;
    }
}
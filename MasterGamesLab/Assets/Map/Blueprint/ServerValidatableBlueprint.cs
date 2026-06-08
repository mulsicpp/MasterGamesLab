using Map.Fleet;
using Map.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using static Map.Blueprint.BlueprintPacket;

namespace Map.Blueprint
{
    public class ServerValidatableBlueprint : ValidatableBlueprint
    {
        private struct EdgeData
        {
            public Edge.EdgeType Type;
            public bool Valid;
            public int Cost;
        }

        private struct StructureData
        {
            public Tile Tile;
            public bool Valid;
            public int Cost;
        }

        private struct VehicleData
        {
            public bool Valid;
            public int Cost;
        }

        private BlueprintPacket blueprintPacket;

        private SortedList<EdgeId, EdgeData> edgeData;
        private SortedList<StructureId, StructureData> structureData;
        private SortedList<VehicleId, VehicleData> vehicleData;


        public ServerValidatableBlueprint(BlueprintPacket blueprintPacket)
        {
            this.blueprintPacket = blueprintPacket;

            edgeData = new();
            foreach (var e in blueprintPacket.Edges)
            {
                edgeData.Add(e.EdgeId, new EdgeData { Type = e.Type });
            }

            structureData = new();
            foreach (var s in blueprintPacket.Structures)
            {
                structureData.Add(s.StructureId, new StructureData { Tile = (Tile)Map.Instance.Tiles[s.TileId] });
            }

            vehicleData = new();
            foreach (var v in blueprintPacket.Vehicles)
            {
                vehicleData.Add(v.VehicleId, new VehicleData { });
            }
        }


        protected override IEnumerable<Edge> EnumerateEdges() => blueprintPacket.Edges.Select(e => Map.Instance.Edges[e.EdgeId]);
        protected override IEnumerable<Structure> EnumerateStructures() => blueprintPacket.Structures.Select(s => Map.Instance.Infrastructure[s.StructureId]);
        protected override IEnumerable<Vehicle> EnumerateVehicles() => blueprintPacket.Vehicles.Select(v => Map.Instance.Fleet[v.VehicleId]);

        protected override void SetValid(Edge edge, bool valid, int cost)
        {
            var data = edgeData[edge.Id];
            data.Valid = valid;
            data.Cost = cost;
            edgeData[edge.Id] = data;
        }

        public override bool IsValid(Edge edge) => edgeData.ContainsKey(edge.Id) ? edgeData[edge.Id].Valid : false;
        public override int Cost(Edge edge) => edgeData.ContainsKey(edge.Id) ? edgeData[edge.Id].Cost : 0;
        public override Edge.EdgeType BlueprintedEdgeType(Edge edge) => edgeData.ContainsKey(edge.Id) ? edgeData[edge.Id].Type : Edge.EdgeType.None;

        protected override void SetValid(Structure structure, bool valid, int cost)
        {
            var data = structureData[structure.Id];
            data.Valid = valid;
            data.Cost = cost;
            structureData[structure.Id] = data;
        }

        public override bool IsValid(Structure structure) => structureData.ContainsKey(structure.Id) ? structureData[structure.Id].Valid : false;
        public override int Cost(Structure structure) => structureData.ContainsKey(structure.Id) ? structureData[structure.Id].Cost : 0;

        public override StructureId BlueprintedStructure(Tile tile)
        {
            foreach (var data in structureData)
            {
                if (data.Value.Tile == tile)
                    return data.Key;
            }
            return StructureId.NONE;
        }

        public override Tile BlueprintedStructureTile(Structure structure) => structureData.ContainsKey(structure.Id) ? structureData[structure.Id].Tile : null;

        protected override void SetValid(Vehicle vehicle, bool valid, int cost)
        {
            var data = vehicleData[vehicle.Id];
            data.Valid = valid;
            data.Cost = cost;
            vehicleData[vehicle.Id] = data;
        }

        public override bool IsValid(Vehicle vehicle) => vehicleData.ContainsKey(vehicle.Id) ? vehicleData[vehicle.Id].Valid : false;
        public override int Cost(Vehicle vehicle) => vehicleData.ContainsKey(vehicle.Id) ? vehicleData[vehicle.Id].Cost : 0;
    }
}
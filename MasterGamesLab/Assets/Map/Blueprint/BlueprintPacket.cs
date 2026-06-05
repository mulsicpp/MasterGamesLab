using Map;
using Map.Infrastructure;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Netcode;

namespace Map.Blueprint
{
    public class BlueprintPacket
    {
        public struct EdgeData : INetworkSerializeByMemcpy
        {
            public EdgeId EdgeId;
            public Edge.EdgeType Type;
        }

        public struct StructureData : INetworkSerializeByMemcpy
        {
            public TileId TileId;
            public StructureId StructureId;
        }

        private int nettoSize;
        public int NettoSize => nettoSize;

        private List<EdgeData> edges;
        public IReadOnlyList<EdgeData> Edges => edges;

        private List<StructureData> structures;
        public IReadOnlyList<StructureData> Structures => structures;

        public BlueprintPacket()
        {
            edges = new();
            structures = new();
            nettoSize = 0;
        }

        public BlueprintPacket(EdgeData[] edges,  StructureData[] structures)
        {
            this.edges = new(edges);
            this.structures = new(structures);
            nettoSize = Marshal.SizeOf<EdgeData>() * edges.Length + Marshal.SizeOf<StructureData>() * structures.Length;
        }

        public void Clear()
        {
            edges.Clear();
            structures.Clear();
            nettoSize = 0;
        }

        public void Append(BlueprintPacket packet)
        {
            edges.AddRange(packet.edges);
            structures.AddRange(packet.structures);

            nettoSize += packet.nettoSize;
        }

        public BlueprintPacket AddEdgeToPackets(Edge edge, List<BlueprintPacket> packets)
        {
            if (edge.BlueprintType == Edge.EdgeType.None) return this;

            var currentPacket = this;
            if(Marshal.SizeOf<EdgeId>() + nettoSize > Constants.MAX_NETTO_BYTES_PER_RPC)
            {
                packets.Add(this);
                currentPacket = new BlueprintPacket();
            }

            switch(edge.BlueprintType)
            {
                case Edge.EdgeType.Road:
                case Edge.EdgeType.Canal:
                    edges.Add(new EdgeData { EdgeId = edge.Id, Type = edge.BlueprintType });
                    break;
                default: return currentPacket;
            }
            nettoSize += Marshal.SizeOf<EdgeData>();
            return currentPacket;
        }

        public BlueprintPacket AddStructureToPackets(Structure structure, List<BlueprintPacket> packets)
        {
            if (structure.BlueprintTile == null) return this;
        
            var currentPacket = this;
            if (Marshal.SizeOf<StructureData>() + nettoSize > Constants.MAX_NETTO_BYTES_PER_RPC)
            {
                packets.Add(this);
                currentPacket = new BlueprintPacket();
            }
        
            switch (structure.Type)
            {
                case Structure.StructureType.Port: structures.Add(new StructureData { StructureId = structure.Id, TileId = structure.BlueprintTile.Id }); break;
                default: return currentPacket;
            }
            nettoSize += Marshal.SizeOf<StructureData>();
            return currentPacket;
        }

        public void Send(bool hasNext)
        {
            Map.Instance.SendBlueprintPacketServerRpc(edges.ToArray(), structures.ToArray(), hasNext);
        }
    } 
}
using Map;
using Map.Infrastructure;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Netcode;

namespace Blueprint
{
    public class BlueprintPacket
    {
        public struct StructureData : INetworkSerializeByMemcpy
        {
            public TileId TileId;
            public StructureIndex StructureIndex;
        }

        private int nettoSize;
        public int NettoSize => nettoSize;

        private List<EdgeId> roadEdgeIds;
        public IReadOnlyList<EdgeId> RoadEdgeIds => roadEdgeIds;

        private List<EdgeId> canalEdgeIds;
        public IReadOnlyList<EdgeId> CanalEdgeIds => canalEdgeIds;

        private List<StructureData> ports;
        public IReadOnlyList<StructureData> Ports => ports;

        public BlueprintPacket()
        {
            roadEdgeIds = new();
            canalEdgeIds = new();
            ports = new();
            nettoSize = 0;
        }

        public BlueprintPacket(EdgeId[] roads, EdgeId[] canals, StructureData[] ports)
        {
            roadEdgeIds = new(roads);
            canalEdgeIds = new(canals);
            this.ports = new(ports);
            nettoSize = Marshal.SizeOf<EdgeId>() * (roads.Length + canals.Length) + Marshal.SizeOf<EdgeId>() * ports.Length;
        }

        public void Clear()
        {
            roadEdgeIds.Clear();
            canalEdgeIds.Clear();
            ports.Clear();
            nettoSize = 0;
        }

        public void Append(BlueprintPacket packet)
        {
            roadEdgeIds.AddRange(packet.roadEdgeIds);
            canalEdgeIds.AddRange(packet.canalEdgeIds);
            ports.AddRange(packet.ports);

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
                case Edge.EdgeType.Road: roadEdgeIds.Add(edge.Id); break;
                case Edge.EdgeType.Canal: canalEdgeIds.Add(edge.Id); break;
                default: return currentPacket;
            }
            nettoSize += Marshal.SizeOf<EdgeId>();
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
                case Structure.StructureType.Port: ports.Add(new StructureData { StructureIndex = structure.Index, TileId = structure.BlueprintTile.Id }); break;
                default: return currentPacket;
            }
            nettoSize += Marshal.SizeOf<StructureData>();
            return currentPacket;
        }

        public void Send(bool hasNext)
        {
            Map.Map.Instance.SendBlueprintPacketServerRpc(roadEdgeIds.ToArray(), canalEdgeIds.ToArray(), ports.ToArray(), hasNext);
        }
    } 
}
using Map;
using Map.Infrastructure;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Blueprint
{
    public class BlueprintPacket
    {
        private int nettoSize;
        public int NettoSize => nettoSize;

        private List<EdgeId> roadEdgeIds;
        public IReadOnlyList<EdgeId> RoadEdgeIds => roadEdgeIds;

        private List<EdgeId> canalEdgeIds;
        public IReadOnlyList<EdgeId> CanalEdgeIds => canalEdgeIds;

        private List<TileId> portTileIds;
        public IReadOnlyList<TileId> PortTileIds => portTileIds;

        public BlueprintPacket()
        {
            roadEdgeIds = new();
            canalEdgeIds = new();
            portTileIds = new();
            nettoSize = 0;
        }

        public BlueprintPacket(EdgeId[] roads, EdgeId[] canals, TileId[] ports)
        {
            roadEdgeIds = new(roads);
            canalEdgeIds = new(canals);
            portTileIds = new(ports);
            nettoSize = Marshal.SizeOf<EdgeId>() * (roads.Length + canals.Length) + Marshal.SizeOf<EdgeId>() * ports.Length;
        }

        public void Clear()
        {
            roadEdgeIds.Clear();
            canalEdgeIds.Clear();
            portTileIds.Clear();
            nettoSize = 0;
        }

        public void Append(BlueprintPacket packet)
        {
            roadEdgeIds.AddRange(packet.roadEdgeIds);
            canalEdgeIds.AddRange(packet.canalEdgeIds);
            portTileIds.AddRange(packet.portTileIds);

            nettoSize += packet.nettoSize;
        }

        public BlueprintPacket AddEdgeToPackets(EdgeId edgeId, List<BlueprintPacket> packets)
        {
            var edge = Map.Map.Instance.Edges[edgeId];
            if (edge.BlueprintType == Edge.EdgeType.None) return this;

            var currentPacket = this;
            if(Marshal.SizeOf<EdgeId>() + nettoSize > Constants.MAX_NETTO_BYTES_PER_RPC)
            {
                packets.Add(this);
                currentPacket = new BlueprintPacket();
            }

            switch(edge.BlueprintType)
            {
                case Edge.EdgeType.Road: roadEdgeIds.Add(edgeId); break;
                case Edge.EdgeType.Canal: canalEdgeIds.Add(edgeId); break;
                default: return currentPacket;
            }
            nettoSize += Marshal.SizeOf<EdgeId>();
            return currentPacket;
        }

        public BlueprintPacket AddStructureToPackets(TileId tileId, List<BlueprintPacket> packets)
        {
            var tile = (Tile)Map.Map.Instance.Tiles[tileId];
            if (tile.BlueprintStructureType == null) return this;

            var currentPacket = this;
            if (Marshal.SizeOf<TileId>() + nettoSize > Constants.MAX_NETTO_BYTES_PER_RPC)
            {
                packets.Add(this);
                currentPacket = new BlueprintPacket();
            }

            switch (tile.BlueprintStructureType)
            {
                case Structure.StructureType.Port: portTileIds.Add(tileId); break;
                default: return currentPacket;
            }
            nettoSize += Marshal.SizeOf<TileId>();
            return currentPacket;
        }

        public void Send(bool hasNext)
        {
            Map.Map.Instance.SendBlueprintPacketServerRpc(roadEdgeIds.ToArray(), canalEdgeIds.ToArray(), portTileIds.ToArray(), hasNext);
        }
    } 
}
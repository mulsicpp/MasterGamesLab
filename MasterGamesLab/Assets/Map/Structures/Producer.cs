
using System.Runtime.InteropServices;
using Unity.Netcode;

namespace Map.Infrastructure
{
    public class Producer : Structure, INetObject<Producer.NetData>
    {
        private Good good;
        public Good Good { get { return good; } set { good = value; Timestamp = Map.Instance.Timestamp; } }

        public struct NetData : INetworkSerializeByMemcpy, INetData
        {
            public byte Offset;
            public TileId TileId;

            public Good Good;

            public StructureId Id { get => new StructureId(StructureType.Producer, Offset); }
            public void SetOffset(byte offset) => Offset = offset;
        }

        public Producer(byte offset, Tile tile, Good good) : base(new StructureId(StructureType.Producer, offset), tile)
        {
            this.good = good;
        }

        public NetData GetNetData() => new NetData { Offset = Id.Offset, TileId = Tile.Id, Good = good };

    }
}
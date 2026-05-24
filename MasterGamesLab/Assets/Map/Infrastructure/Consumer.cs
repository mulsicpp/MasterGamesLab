
using Unity.Netcode;

namespace Map.Infrastructure
{
    public class Consumer : Structure, INetObject<Consumer.NetData>
    {
        private Good requestedGood;
        public Good RequestedGood { get { return requestedGood; } set { requestedGood = value; Timestamp = Map.Instance.Timestamp; } }

        public struct NetData : INetworkSerializeByMemcpy, INetData
        {
            public byte Offset;
            public TileId TileId;

            public Good RequestedGood;

            public StructureId Id { get => new StructureId(StructureType.Consumer, Offset); }
            public void SetOffset(byte offset) => Offset = offset;
        }

        public Consumer(byte offset, Tile tile, Good requestedGood) : base(new StructureId(StructureType.Consumer, offset), tile)
        {
            this.requestedGood = requestedGood;
        }

        public NetData GetNetData() => new NetData { Offset = Id.Offset, TileId = Tile.Id, RequestedGood = requestedGood };

    }
}
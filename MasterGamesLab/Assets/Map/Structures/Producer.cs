
using Unity.Netcode;

namespace Map.Structures
{
    public class Producer : Structure
    {
        private Good good;
        public Good Good { get { return good; } set { good = value; Timestamp = Map.Instance.Timestamp; } }

        public struct NetData : INetworkSerializeByMemcpy
        {
            public StructureId Id;
            public TileId TileId;

            public Good Good;
        }

        public Producer(byte offset, Tile tile, Good good) : base(new StructureId(StructureType.Producer, offset), tile)
        {
            this.good = good;
        }

        public NetData GetNetData() => new NetData { Id = Id, TileId = Tile.Id, Good = good };
    }

}

using Unity.Netcode;

namespace Map.Infrastructure
{
    public class Consumer : Structure, INetObject<Consumer.NetData>
    {
        public override StructureType Type => StructureType.Consumer;

        private Good requestedGood;
        public Good RequestedGood { get { return requestedGood; } set { requestedGood = value; Timestamp = Map.Instance.Timestamp; } }

        public struct NetData : INetworkSerializeByMemcpy, INetData
        {
            public StructureIndex Index;
            public TileId TileId;

            public Good RequestedGood;

            public StructureType Type => StructureType.Consumer;
            public void SetIndex(StructureIndex index) => Index = index;
        }

        public Consumer(StructureIndex index) : base(index)
        {
            requestedGood = Good.None;
        }

        public NetData GetNetData() => new NetData { Index = Index, TileId = Tile.Id, RequestedGood = requestedGood };

    }
}
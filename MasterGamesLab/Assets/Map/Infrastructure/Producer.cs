
using System.Runtime.InteropServices;
using Unity.Netcode;
using static Map.Infrastructure.Structure;

namespace Map.Infrastructure
{
    public class Producer : Structure, INetObject<Producer.NetData>
    {
        public override StructureType Type => StructureType.Producer;

        private Good good;
        public Good Good { get { return good; } set { good = value; Timestamp = Map.Instance.Timestamp; } }

        public struct NetData : INetworkSerializeByMemcpy, INetData
        {
            public StructureIndex Index;
            public TileId TileId;

            public Good Good;

            public StructureType Type => StructureType.Producer;
            public void SetIndex(StructureIndex index) => Index = index;
        }

        public Producer(StructureIndex index) : base(index)
        {
            good = Good.None;
        }

        public NetData GetNetData() => new NetData { Index = Index, TileId = Tile.Id, Good = good };
    }
}
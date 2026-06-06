using Networking;
using Unity.Netcode;

namespace Map.Player
{
    public class Player : Timestamped, ISynchableObject<Player.PlayerState>
    {
        public struct PlayerState : IState, INetworkSerializeByMemcpy
        {
            public PlayerId Id;
            public int Money;

            public int ArrayIndex { get => Id; set => Id = new PlayerId((byte)value); }

            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public readonly PlayerId Id;

        public new Timestamp Timestamp => base.Timestamp;

        private int money;
        public int Money
        {
            get => money;
            set { money = value; Touch(); }
        }

        public PlayerState State
        {
            get => new PlayerState { Id = Id, Money = Money };
            set { Money = value.Money; }
        }

        public Player(PlayerId id)
        {
            Id = id;
            money = 0;
            Touch();
        }

        public void ApplyServerState(PlayerState state, double _) { State = state; ResetDirty(); }
    }
}
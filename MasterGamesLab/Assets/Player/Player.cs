using Networking;
using System;
using Unity.Netcode;
using UnityEngine;

namespace Player
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

        internal static PlayerId selfId = PlayerId.NONE;
        public static PlayerId SelfId => selfId;

        public static event Action<Player> OnPlayerChanged;

        public readonly PlayerId Id;
        public bool IsSelf => Id == SelfId;

        public new Map.Timestamp Timestamp => base.Timestamp;

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
            money = Constants.PLAYER_START_MONEY;
            Touch();
        }

        public override void Touch()
        {
            base.Touch();
            OnPlayerChanged?.Invoke(this);
        }

        public void ApplyServerState(PlayerState state, double _) { State = state; ResetDirty(); }
    }
}
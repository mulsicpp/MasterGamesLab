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
            public int Cash;
            public int Revenue;

            public int ArrayIndex { get => Id; set => Id = new PlayerId((byte)value); }

            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        internal static PlayerId selfId = PlayerId.NONE;
        public static PlayerId SelfId => selfId;

        public static event Action<Player> OnPlayerChanged;

        public readonly PlayerId Id;
        public bool IsSelf => Id == SelfId;

        public new Map.Timestamp Timestamp => base.Timestamp;

        private int cash;
        public int Cash
        {
            get => cash;
            set { cash = value; Touch(); }
        }

        private int revenue;
        public int Revenue
        {
            get => revenue;
            private set { revenue = value; Touch(); }
        }

        public PlayerState State
        {
            get => new PlayerState { Id = Id, Cash = Cash };
            set { Cash = value.Cash; Revenue = value.Revenue; }
        }

        public Player(PlayerId id)
        {
            Id = id;
            cash = Constants.PLAYER_INITIAL_CASH;
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
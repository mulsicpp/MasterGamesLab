
using Unity.Netcode;
using Networking;
using UnityEngine;

namespace Map.Infrastructure
{
    public class Consumer : Structure, ISynchableObject<Consumer.ConsumerState>
    {
        public struct ConsumerState : IState, IStructureState, INetworkSerializeByMemcpy
        {
            public CommonStructureState Common;
            public Good RequestedGood;
            public int CurrentPayout;

            public StructureType Type => StructureType.Consumer;

            public int ArrayIndex { get => Common.ArrayIndex; set => Common.ArrayIndex = value; }
            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public override StructureType Type => StructureType.Consumer;

        private Good requestedGood;
        public Good RequestedGood { get { return requestedGood; } set { requestedGood = value; Touch(); TriggerDirty(); } }

        private int currentPayout;
        public int CurrentPayout { get { return currentPayout; } set { currentPayout = value; Touch(); TriggerDirty(); } }

        private float requestCooldown;

        private float payoutIncreaseCooldown;
        private int nextPayout;

        public ConsumerState State
        {
            get => new ConsumerState { Common = CommonState, RequestedGood = RequestedGood, CurrentPayout = CurrentPayout };
            set { CommonState = value.Common; RequestedGood = value.RequestedGood; CurrentPayout = value.CurrentPayout; }
        }

        public Consumer(StructureIndex index) : base(index)
        {
            requestedGood = Good.None;
        }

        public void ApplyServerState(ConsumerState state, double _) { State = state; ResetDirty(); }

        public override void OnStructureSpawned()
        {
            ClearRequest();
        }

        public override void Tick(float tickDuration)
        {
            if (!Exists) return;
            if (RequestedGood == Good.None && (requestCooldown -= tickDuration) <= 0)
            {
                RequestedGood = (Good) Random.Range((int)Good.Apple, (int)Good.Banana + 1);
                CurrentPayout = Constants.CONSUMER_REQUEST_BASE_PAYOUT;
                payoutIncreaseCooldown = NextPayoutIncreaseCooldown();
                nextPayout = (int)(CurrentPayout * NextPayoutIncreaseFactor());
                return;
            }

            if ((payoutIncreaseCooldown -= tickDuration) <= 0)
            {
                CurrentPayout = nextPayout;
                payoutIncreaseCooldown = NextPayoutIncreaseCooldown();
                nextPayout = (int)(CurrentPayout * NextPayoutIncreaseFactor());
            }
        }

        public void ClearRequest()
        {
            RequestedGood = Good.None;

            requestCooldown = NextRequestCooldown();
        }

        private float NextRequestCooldown()
        {
            return Random.Range(Constants.MIN_CONSUMER_REQUEST_COOLDOWN, Constants.MAX_CONSUMER_REQUEST_COOLDOWN);
        }

        private float NextPayoutIncreaseCooldown()
        {
            return Random.Range(Constants.MIN_CONSUMER_PAYOUT_INCREASE_COOLDOWN, Constants.MAX_CONSUMER_PAYOUT_INCREASE_COOLDOWN);
        }

        private float NextPayoutIncreaseFactor()
        {
            return Random.Range(Constants.MIN_CONSUMER_PAYOUT_INCREASE_FACTOR, Constants.MAX_CONSUMER_PAYOUT_INCREASE_FACTOR);
        }
    }
}
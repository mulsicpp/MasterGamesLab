
using Unity.Netcode;
using Networking;
using UnityEngine;
using Map.GeometryGeneration;
using System.Security.Policy;
using Map.Fleet;

namespace Map.Infrastructure
{
    public class Consumer : Structure, ISynchableObject<Consumer.ConsumerState>
    {
        public struct ConsumerState : IState, IStructureState, INetworkSerializeByMemcpy
        {
            public CommonStructureState Common;
            public ConsumerRequest Request;

            public StructureType Type => StructureType.Consumer;

            public int ArrayIndex { get => Common.ArrayIndex; set => Common.ArrayIndex = value; }
            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public struct ConsumerRequest : INetworkSerializeByMemcpy
        {
            public Good Good;
            public int Payout;

            public ConsumerRequest(Good good, int payout)
            {
                Good = good;
                Payout = payout;
            }
        }

        public override StructureType Type => StructureType.Consumer;

        public override GameObject StructurePrefab => Map.Instance.ConsumerPrefab;

        private ConsumerRequest request;
        public ConsumerRequest Request { get { return request; } set { request = value; Touch(); TriggerRendererUpdate(); } }

        private float requestCooldown;

        private float payoutIncreaseCooldown;
        private int nextPayout;

        public ConsumerState State
        {
            get => new ConsumerState { Common = CommonState, Request = Request };
            set { CommonState = value.Common; Request = value.Request; }
        }

        public Consumer(StructureIndex index) : base(index)
        {
            request = new(Good.None, 0);
        }

        public void ApplyServerState(ConsumerState state, double _) { State = state; ResetDirty(); }


        public override ObjectWithFixedGeometry AttachStructureGeometry(Transform parent)
        {
            var id = Tile?.Id ?? BlueprintTile.Id;
            return GeometriesManager.Instance.GetGameObjectGeometry(GeometriesManager.GeometryType.Consumer, id, parent);
        }


        public override void OnStructureSpawned()
        {
            Request = new(Good.None, 0);
        }

        public override void Tick(float tickDuration)
        {
            if (!Exists || Request.Good == Good.None) return;

            if ((payoutIncreaseCooldown -= tickDuration) <= 0)
            {
                request = new(Request.Good, nextPayout);
                SetupPayoutIncrease();
            }
        }

        public void SetupPayoutIncrease()
        {
            payoutIncreaseCooldown = NextPayoutIncreaseCooldown();
            nextPayout = (int)(Request.Payout * NextPayoutIncreaseFactor());
        }

        public void FulfillRequest(Truck truck)
        {
            truck.Owner.Earn(Request.Payout);
            truck.Good = Good.None;
            Map.Instance.SpawnLogic.ClearConsumerRequest(this);
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
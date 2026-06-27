using Map.GeometryGeneration;
using Networking;
using Unity.Netcode;
using UnityEngine;
using static Map.Infrastructure.Producer;

namespace Map.Infrastructure
{
    public class CarPark : Structure, ISynchableObject<CarPark.CarParkState>
    {
        public struct CarParkState : IState, IStructureState, INetworkSerializeByMemcpy
        {
            public CommonStructureState Common;

            public StructureType Type => StructureType.CarPark;

            public int ArrayIndex
            {
                get => Common.ArrayIndex;
                set => Common.ArrayIndex = value;
            }

            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public override StructureType Type => StructureType.CarPark;

        public override GameObject StructurePrefab => Map.Instance.CarParkPrefab;

        public CarParkState State
        {
            get => new CarParkState { Common = CommonState };
            set { CommonState = value.Common; }
        }

        public CarPark(StructureIndex index) : base(index)
        {
        }

        public void ApplyServerState(CarParkState state, double _)
        {
            State = state;
            ResetDirty();
        }

        public override ObjectWithFixedGeometry AttachStructureGeometry(Transform parent)
        {
            var id = Tile?.Id ?? BlueprintTile.Id;
            return GeometriesManager.Instance.GetGameObjectGeometry(GeometriesManager.GeometryType.ParkingLot, id,
                parent);
        }
    }
}
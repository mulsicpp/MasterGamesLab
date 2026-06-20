using Map.GeometryGeneration;
using Networking;
using Unity.Netcode;
using UnityEngine;
using static Map.Infrastructure.Producer;

namespace Map.Infrastructure
{
    public class Garage : Structure, ISynchableObject<Garage.GarageState>
    {
        public struct GarageState : IState, IStructureState, INetworkSerializeByMemcpy
        {
            public CommonStructureState Common;

            public StructureType Type => StructureType.Garage;

            public int ArrayIndex { get => Common.ArrayIndex; set => Common.ArrayIndex = value; }
            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public override StructureType Type => StructureType.Garage;

        public override GameObject StructurePrefab => Map.Instance.GaragePrefab;

        public GarageState State
        {
            get => new GarageState { Common = CommonState };
            set { CommonState = value.Common; }
        }

        public Garage(StructureIndex index) : base(index)
        { }

        public void ApplyServerState(GarageState state, double _) { State = state; ResetDirty(); }

        public override ObjectWithFixedGeometry AttachStructureGeometry(Transform parent)
        {
            var id = Tile?.Id ?? BlueprintTile.Id;
            return GeometriesManager.Instance.GetGameObjectGeometry(GeometriesManager.GeometryType.Consumer, id, parent);
        }
    }
}
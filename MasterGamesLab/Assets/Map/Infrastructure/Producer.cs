using System;
using Unity.Netcode;
using Networking;
using Map.GeometryGeneration;
using UnityEngine;

namespace Map.Infrastructure
{
    public class Producer : Structure, ISynchableObject<Producer.ProducerState>
    {
        public struct ProducerState : IState, IStructureState, INetworkSerializeByMemcpy
        {
            public CommonStructureState Common;
            public Good Good;

            public StructureType Type => StructureType.Producer;

            public int ArrayIndex
            {
                get => Common.ArrayIndex;
                set => Common.ArrayIndex = value;
            }

            public int SerializedSize => FastBufferWriter.GetWriteSize(this);
        }

        public override StructureType Type => StructureType.Producer;

        public override GameObject StructurePrefab => Map.Instance.ProducerPrefab;

        private Good good;

        public Good Good
        {
            get { return good; }
            set
            {
                good = value;
                Touch();
                TriggerRendererRebuild();
            }
        }

        public ProducerState State
        {
            get => new ProducerState { Common = CommonState, Good = Good };
            set
            {
                CommonState = value.Common;
                Good = value.Good;
            }
        }

        public Producer(StructureIndex index) : base(index)
        {
            good = Good.None;
        }

        public void ApplyServerState(ProducerState state, double _)
        {
            State = state;
            ResetDirty();
        }

        public override ObjectWithFixedGeometry AttachStructureGeometry(Transform parent)
        {
            var id = Tile?.Id ?? BlueprintTile.Id;
            var type = good switch
            {
                Good.Common => GeometriesManager.GeometryType.ProducerTetrahedron,
                Good.Uncommon => GeometriesManager.GeometryType.ProducerCube,
                Good.Rare => GeometriesManager.GeometryType.ProducerOctahedron,
                Good.Epic => GeometriesManager.GeometryType.ProducerIcosahedron,
                Good.Legendary => GeometriesManager.GeometryType.ProducerDodecahedron,
                Good.None => GeometriesManager.GeometryType.ProducerTetrahedron,
                _ => throw new ArgumentOutOfRangeException()
            };

            return GeometriesManager.Instance.GetGameObjectGeometry(type, id, parent);
        }
    }
}
using System;
using System.Collections.Generic;

namespace Map.Infrastructure
{
    public class Infrastructure : IReadOnlyInfrastructure
    {
        private Producer[] producers;
        public IReadOnlyList<Producer> Producers => producers;

        private Consumer[] consumers;
        public IReadOnlyList<Consumer> Consumers => consumers;

        // Garage[] garages;
        // public IReadOnlyList<Garage> Garages => garages;

        // Port[] ports;
        // public IReadOnlyList<Port> Ports => ports;

        // TrainStation[] trainStations;
        // public IReadOnlyList<TrainStation> TrainStations => trainStations;

        public Infrastructure()
        {
            producers = new Producer[Constants.MAX_PRODUCER_COUNT];
            for (var i = 0; i < producers.Length; i++) producers[i] = new Producer(new StructureIndex((byte)i));

            consumers = new Consumer[Constants.MAX_CONSUMER_COUNT];
            for (var i = 0; i < consumers.Length; i++) consumers[i] = new Consumer(new StructureIndex((byte)i));
        }

        public int GetFirstEmptyIndex(Structure.StructureType type)
        {
            Structure[] structures = type switch
            {
                Structure.StructureType.Producer => producers,
                Structure.StructureType.Consumer => consumers,
                _ => null
            };

            if (structures == null) return -1;

            for (int i = 0; i < structures.Length; i++)
            {
                if (!structures[i].Exists)
                    return i;
            }

            return Test();

            int Test()
            {
                var oo = 0;
                for (var i = 0; i < 10; i++)
                {
                    oo += i;
                }

                return oo;
            }
        }

        public void UpdateStructure<T>(T state) where T : struct, Structure.IStructureState
        {
            if (state is Producer.ProducerState p) producers[p.ArrayIndex].State = p;
            else if (state is Consumer.ConsumerState c) consumers[c.ArrayIndex].State = c;
            else throw new ArgumentException("Given IStructureState is not supported: " + state.GetType().FullName);
        }

        public bool SpawnLocal<T>(T state) where T : struct, Structure.IStructureState
        {
            int index = GetFirstEmptyIndex(state.Type);
            if (index > -1)
            {
                UpdateStructure(state);
                return true;
            }
            return false;
        }

        public bool SpawnGlobal<T>(T state) where T : struct, Structure.IStructureState
        {
            int index = GetFirstEmptyIndex(state.Type);
            if (index > -1)
            {
                state.ArrayIndex = index;

                Map.Instance.ReliableSender.Add(state);
                Map.Instance.ReliableSender.Send();

                return true;
            }
            return false;
        }
    }
}
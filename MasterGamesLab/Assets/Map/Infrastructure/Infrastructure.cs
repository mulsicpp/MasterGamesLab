using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Qos.V2.Models;

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
            Structure[] structures = null;

            switch (type)
            {
                case Structure.StructureType.Producer: structures = producers; break;
                case Structure.StructureType.Consumer: structures = consumers; break;
                // case Structure.StructureType.Garage: structures = Garages; break;
                // case Structure.StructureType.Port: structures = Ports; break;
                // case Structure.StructureType.TrainStation: structures = TrainStations; break;
            }

            if (structures == null) return -1;

            for (int i = 0; i < structures.Length; i++)
            {
                if (!structures[i].Exists)
                    return i;
            }
            return -1;
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

                var nextTimestamp = Map.Instance.Timestamp.Next();
                Map.Instance.UpdateGenericStatesClient(nextTimestamp, new[] { state });
                return true;
            }
            return false;
        }
    }
}
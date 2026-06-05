using System;
using System.Collections.Generic;
using UnityEngine;

namespace Map.Infrastructure
{
    public class Infrastructure : IReadOnlyInfrastructure
    {
        private Producer[] producers;
        public IReadOnlyList<Producer> Producers => producers;

        private Consumer[] consumers;
        public IReadOnlyList<Consumer> Consumers => consumers;

        Garage[] garages;
        public IReadOnlyList<Garage> Garages => garages;

        Port[] ports;
        public IReadOnlyList<Port> Ports => ports;

        // TrainStation[] trainStations;
        // public IReadOnlyList<TrainStation> TrainStations => trainStations;

        public Infrastructure()
        {
            producers = new Producer[Constants.MAX_PRODUCER_COUNT];
            for (var i = 0; i < producers.Length; i++) producers[i] = new Producer(new StructureIndex((byte)i));

            consumers = new Consumer[Constants.MAX_CONSUMER_COUNT];
            for (var i = 0; i < consumers.Length; i++) consumers[i] = new Consumer(new StructureIndex((byte)i));

            garages = new Garage[Constants.MAX_GARAGE_COUNT];
            for (var i = 0; i < garages.Length; i++) garages[i] = new Garage(new StructureIndex((byte)i));

            ports = new Port[Constants.MAX_PORT_COUNT];
            for (var i = 0; i < ports.Length; i++) ports[i] = new Port(new StructureIndex((byte)i));
        }

        public Structure this[StructureId id] => this[id.Type]?[id.Index];

        public IReadOnlyList<Structure> this[Structure.StructureType type]
        {
            get
            {
                return type switch
                {
                    Structure.StructureType.Producer => producers,
                    Structure.StructureType.Consumer => consumers,
                    Structure.StructureType.Garage => garages,
                    Structure.StructureType.Port => ports,
                    // case Structure.StructureType.TrainStation: structures = trainStations; break;
                    _ => null
                };
            }
        }

        public Structure GetFirstWith(Structure.StructureType type, Predicate<Structure> condition = null)
        {
            condition ??= s => !s.Exists;

            var structures = this[type];

            Debug.Log("structures: " + structures);
            if (structures == null) return null;

            for (int i = 0; i < structures.Count; i++)
            {
                if (condition(structures[i]))
                    return structures[i];
            }

            return null;
        }

        public void UpdateStructure<T>(T state) where T : struct, Structure.IStructureState
        {
            if (state is Producer.ProducerState p) producers[p.ArrayIndex].State = p;
            else if (state is Consumer.ConsumerState c) consumers[c.ArrayIndex].State = c;
            else if (state is Port.PortState pt) ports[pt.ArrayIndex].State = pt;
            else if (state is Garage.GarageState g) garages[g.ArrayIndex].State = g;
            else throw new ArgumentException("Given IStructureState is not supported: " + state.GetType().FullName);
        }

        public bool SpawnLocal<T>(T state, PlayerId owner) where T : struct, Structure.IStructureState
        {
            var structure = GetFirstWith(state.Type, s => !s.Exists && s.Owner == owner);
            if (structure != null)
            {
                state.ArrayIndex = structure.Index;
                UpdateStructure(state);
                return true;
            }

            return false;
        }

        public bool SpawnGlobal<T>(T state, PlayerId owner) where T : struct, Structure.IStructureState
        {
            var structure = GetFirstWith(state.Type, s => !s.Exists && s.Owner == owner);
            if (structure != null)
            {
                state.ArrayIndex = structure.Index;

                Map.Instance.ReliableSender.Add(state);
                Map.Instance.ReliableSender.Send();

                return true;
            }

            return false;
        }
    }
}
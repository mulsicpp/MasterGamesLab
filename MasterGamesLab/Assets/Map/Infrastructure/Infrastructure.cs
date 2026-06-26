using System;
using System.Collections.Generic;
using UnityEngine;

namespace Map.Infrastructure
{
    public class Infrastructure : IReadOnlyInfrastructure
    {
        private readonly Producer[] producers;
        public IReadOnlyList<Producer> Producers => producers;

        private readonly Consumer[] consumers;
        public IReadOnlyList<Consumer> Consumers => consumers;

        private readonly CarPark[] carParks;
        public IReadOnlyList<CarPark> CarParks => carParks;

        private readonly Port[] ports;
        public IReadOnlyList<Port> Ports => ports;

        private readonly Structure[] structures;
        public IReadOnlyList<Structure> Structures => structures;

        private readonly Dictionary<Structure.StructureType, Range> structureRanges;
        public IReadOnlyDictionary<Structure.StructureType, Range> StructureRanges => structureRanges;

        // TrainStation[] trainStations;
        // public IReadOnlyList<TrainStation> TrainStations => trainStations;

        public Infrastructure(int playerCount)
        {
            var tempStructures = new List<Structure>();

            structureRanges = new();

            producers = new Producer[Constants.MAX_PRODUCER_COUNT];
            for (var i = 0; i < producers.Length; i++) producers[i] = new Producer(new StructureIndex((byte)i));
            structureRanges[Structure.StructureType.Producer] = tempStructures.Count..(tempStructures.Count + producers.Length);
            tempStructures.AddRange(producers);

            consumers = new Consumer[Constants.MAX_CONSUMER_COUNT];
            for (var i = 0; i < consumers.Length; i++) consumers[i] = new Consumer(new StructureIndex((byte)i));
            structureRanges[Structure.StructureType.Consumer] = tempStructures.Count..(tempStructures.Count + consumers.Length);
            tempStructures.AddRange(consumers);

            carParks = new CarPark[Constants.MAX_GARAGES_PER_PLAYER * playerCount];
            for (var i = 0; i < carParks.Length; i++) carParks[i] = new CarPark(new StructureIndex((byte)i));
            structureRanges[Structure.StructureType.CarPark] = tempStructures.Count..(tempStructures.Count + carParks.Length);
            tempStructures.AddRange(carParks);

            ports = new Port[Constants.MAX_PORTS_PER_PLAYER * playerCount];
            for (var i = 0; i < ports.Length; i++) ports[i] = new Port(new StructureIndex((byte)i));
            structureRanges[Structure.StructureType.Port] = tempStructures.Count..(tempStructures.Count + ports.Length);
            tempStructures.AddRange(ports);

            structures = tempStructures.ToArray();
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
                    Structure.StructureType.CarPark => carParks,
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
            else if (state is CarPark.CarParkState g) carParks[g.ArrayIndex].State = g;
            else throw new ArgumentException("Given IStructureState is not supported: " + state.GetType().FullName);
        }

        public Structure SpawnLocal<T>(T state, Player.Player owner = null) where T : struct, Structure.IStructureState
        {
            var structure = GetFirstWith(state.Type, s => !s.Exists && s.Owner == owner);
            if (structure != null)
            {
                state.ArrayIndex = structure.Index;
                UpdateStructure(state);
                return structure;
            }

            return null;
        }

        public Structure SpawnGlobal<T>(T state, Player.Player owner = null) where T : struct, Structure.IStructureState
        {
            var structure = GetFirstWith(state.Type, s => !s.Exists && s.Owner == owner);
            if (structure != null)
            {
                state.ArrayIndex = structure.Index;

                Map.Instance.ReliableSender.Add(state);
                Map.Instance.ReliableSender.Send();

                return structure;
            }

            return null;
        }
    }
}
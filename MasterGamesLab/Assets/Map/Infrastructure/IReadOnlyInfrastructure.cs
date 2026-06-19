using System;
using System.Collections.Generic;

namespace Map.Infrastructure
{
    public interface IReadOnlyInfrastructure
    {
        public IReadOnlyList<Producer> Producers { get; }
        public IReadOnlyList<Consumer> Consumers { get; }
        public IReadOnlyList<Garage> Garages { get; }
        public IReadOnlyList<Port> Ports { get; }

        public IReadOnlyList<Structure> Structures { get; }
        // public IReadOnlyList<TrainStation> TrainStations { get; }

        public IReadOnlyDictionary<Structure.StructureType, Range> StructureRanges { get; }

        public Structure this[StructureId id] { get; }
        public IReadOnlyList<Structure> this[Structure.StructureType type] { get; }

        public Structure GetFirstWith(Structure.StructureType type, Predicate<Structure> condition = null);

        public Structure SpawnLocal<T>(T state, Player.Player owner = null) where T : struct, Structure.IStructureState;

        public Structure SpawnGlobal<T>(T state, Player.Player owner = null) where T : struct, Structure.IStructureState;
    }
}
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
        // public IReadOnlyList<TrainStation> TrainStations { get; }

        public Structure this[StructureId id] { get; }
        public IReadOnlyList<Structure> this[Structure.StructureType type] { get; }

        public Structure GetFirstWith(Structure.StructureType type, Predicate<Structure> condition = null);

        public bool SpawnLocal<T>(T state, Player.Player owner = null) where T : struct, Structure.IStructureState;

        public bool SpawnGlobal<T>(T state, Player.Player owner = null) where T : struct, Structure.IStructureState;
    }
}
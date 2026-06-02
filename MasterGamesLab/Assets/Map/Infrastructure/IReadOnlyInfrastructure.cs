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

        public bool SpawnLocal<T>(T state) where T : struct, Structure.IStructureState;
        public bool SpawnGlobal<T>(T state) where T : struct, Structure.IStructureState;
    }
}
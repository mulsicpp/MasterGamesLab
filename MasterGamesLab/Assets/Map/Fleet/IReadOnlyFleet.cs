using Map.Infrastructure;
using System.Collections.Generic;

namespace Map.Fleet
{
    public interface IReadOnlyFleet
    {
        public IReadOnlyList<Truck> Trucks { get; }
        public IReadOnlyList<Freighter> Freighters { get; }
        public IReadOnlyList<Vehicle> Vehicles { get; }

        public bool SpawnLocal<T>(T state) where T : struct, Vehicle.IVehicleState;
        public bool SpawnGlobal<T>(T state) where T : struct, Vehicle.IVehicleState;
    }
}
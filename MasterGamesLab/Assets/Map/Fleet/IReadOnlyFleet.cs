using Map.Infrastructure;
using System;
using System.Collections.Generic;

namespace Map.Fleet
{
    public interface IReadOnlyFleet
    {
        public IReadOnlyList<Truck> Trucks { get; }
        public IReadOnlyList<Freighter> Freighters { get; }

        public IReadOnlyList<Vehicle> Vehicles { get; }

        public IReadOnlyDictionary<Vehicle.VehicleType, Range> VehicleRanges { get; }

        public Vehicle this[VehicleId id] { get; }
        public IReadOnlyList<Vehicle> this[Vehicle.VehicleType type] { get; }

        public Vehicle GetFirstWith(Vehicle.VehicleType type, Predicate<Vehicle> condition = null);

        public Vehicle SpawnLocal<T>(T state, Player.Player owner) where T : struct, Vehicle.IVehicleState;
        public Vehicle SpawnGlobal<T>(T state, Player.Player owner) where T : struct, Vehicle.IVehicleState;
    }
}
using Map.Infrastructure;
using System;
using System.Collections.Generic;

namespace Map.Fleet
{
    public class Fleet : IReadOnlyFleet
    {
        private readonly Truck[] trucks;
        public IReadOnlyList<Truck> Trucks => trucks;

        private readonly Freighter[] freighters;
        public IReadOnlyList<Freighter> Freighters => freighters;

        private readonly Vehicle[] vehicles;
        public IReadOnlyList<Vehicle> Vehicles => vehicles;

        private readonly Dictionary<Vehicle.VehicleType, Range> vehicleRanges;
        public IReadOnlyDictionary<Vehicle.VehicleType, Range> VehicleRanges => vehicleRanges;

        public Fleet(int playerCount)
        {
            var tempVehicles = new List<Vehicle>();
            vehicleRanges = new();

            trucks = new Truck[Constants.MAX_TRUCKS_PER_PLAYER * playerCount];
            for (int i = 0; i < trucks.Length; i++) trucks[i] = new Truck(new VehicleIndex((byte)i));
            vehicleRanges[Vehicle.VehicleType.Truck] = tempVehicles.Count..(tempVehicles.Count + trucks.Length);
            tempVehicles.AddRange(trucks);

            freighters = new Freighter[Constants.MAX_FREIGHTERS_PER_PLAYER * playerCount];
            for (int i = 0; i < freighters.Length; i++) freighters[i] = new Freighter(new VehicleIndex((byte)i));
            vehicleRanges[Vehicle.VehicleType.Freighter] = tempVehicles.Count..(tempVehicles.Count + freighters.Length);
            tempVehicles.AddRange(freighters);

            vehicles = tempVehicles.ToArray();
        }

        public Vehicle this[VehicleId id] => this[id.Type]?[id.Index];
        public IReadOnlyList<Vehicle> this[Vehicle.VehicleType type]
        {
            get
            {
                return type switch {
                    Vehicle.VehicleType.Truck => trucks,
                    Vehicle.VehicleType.Freighter => freighters,
                    _ => null
                };
            }
        }

        public Vehicle GetFirstWith(Vehicle.VehicleType type, Predicate<Vehicle> condition = null)
        {
            condition ??= v => !v.Exists;

            var vehicles = this[type];

            if (vehicles == null) return null;

            for (int i = 0; i < vehicles.Count; i++)
            {
                if (condition(vehicles[i]))
                    return vehicles[i];
            }

            return null;
        }

        public void UpdateVehicle<T>(T state) where T : struct, Vehicle.IVehicleState
        {
            if (state is Truck.TruckState t) trucks[t.ArrayIndex].State = t;
            else if (state is Freighter.FreighterState f) freighters[f.ArrayIndex].State = f;
            else throw new ArgumentException("Given IVehicleState is not supported: " + state.GetType().FullName);
        }


        public Vehicle SpawnLocal<T>(T state, Player.Player owner = null) where T : struct, Vehicle.IVehicleState
        {
            var vehicle = GetFirstWith(state.Type, v => !v.Exists && v.Owner == owner);
            if (vehicle != null)
            {
                state.ArrayIndex = vehicle.Index;
                UpdateVehicle(state);
                return vehicle;
            }

            return null;
        }

        public Vehicle SpawnGlobal<T>(T state, Player.Player owner) where T : struct, Vehicle.IVehicleState
        {
            var vehicle = GetFirstWith(state.Type, v => !v.Exists && v.Owner == owner);
            if (vehicle != null)
            {
                state.ArrayIndex = vehicle.Index;

                Map.Instance.ReliableSender.Add(state);
                Map.Instance.ReliableSender.Send();

                return vehicle;
            }

            return null;
        }
    }
}
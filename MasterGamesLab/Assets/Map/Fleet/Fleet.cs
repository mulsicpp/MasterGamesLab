using Map.Infrastructure;
using System;
using System.Collections.Generic;

namespace Map.Fleet
{
    public class Fleet : IReadOnlyFleet
    {
        private Truck[] trucks;
        public IReadOnlyList<Truck> Trucks => trucks;

        private Freighter[] freighters;
        public IReadOnlyList<Freighter> Freighters => freighters;

        private Vehicle[] vehicles;
        public IReadOnlyList<Vehicle> Vehicles => vehicles;

        public Fleet()
        {
            trucks = new Truck[Constants.MAX_TRUCK_COUNT];
            for (int i = 0; i < trucks.Length; i++) trucks[i] = new Truck(new VehicleIndex((byte)i));

            freighters = new Freighter[Constants.MAX_FREIGHTER_COUNT];
            for (int i = 0; i < freighters.Length; i++) freighters[i] = new Freighter(new VehicleIndex((byte)i));

            vehicles = new Vehicle[trucks.Length + freighters.Length];
            Array.Copy(trucks, 0, vehicles, 0, trucks.Length);
            Array.Copy(freighters, 0, vehicles, trucks.Length, freighters.Length);
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


        public bool SpawnLocal<T>(T state, Player.Player owner = null) where T : struct, Vehicle.IVehicleState
        {
            var vehicle = GetFirstWith(state.Type, v => !v.Exists && v.Owner == owner);
            if (vehicle != null)
            {
                state.ArrayIndex = vehicle.Index;
                UpdateVehicle(state);
                return true;
            }

            return false;
        }

        public bool SpawnGlobal<T>(T state, Player.Player owner) where T : struct, Vehicle.IVehicleState
        {
            var vehicle = GetFirstWith(state.Type, v => !v.Exists && v.Owner == owner);
            if (vehicle != null)
            {
                state.ArrayIndex = vehicle.Index;

                Map.Instance.ReliableSender.Add(state);
                Map.Instance.ReliableSender.Send();

                return true;
            }

            return false;
        }
    }
}
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

        public int GetFirstEmptyIndex(Vehicle.VehicleType type, PlayerId owner)
        {
            Vehicle[] vehicles = null;

            switch (type)
            {
                case Vehicle.VehicleType.Truck: vehicles = trucks; break;
                case Vehicle.VehicleType.Freighter: vehicles = freighters; break;
            }

            if (vehicles == null) return -1;

            int countPerPlayer = Vehicle.GetMaxCountPerPlayer(type);
            for (int i = owner * countPerPlayer; i < ((int)owner + 1) * countPerPlayer; i++)
            {
                if (!vehicles[i].Exists)
                    return i;
            }
            return -1;
        }

        public void UpdateVehicle<T>(T state) where T : struct, Vehicle.IVehicleState
        {
            if (state is Truck.TruckState t) trucks[t.ArrayIndex].State = t;
            else if (state is Freighter.FreighterState f) freighters[f.ArrayIndex].State = f;
            else throw new ArgumentException("Given IVehicleState is not supported: " + state.GetType().FullName);
        }


        public bool SpawnLocal<T>(T state, PlayerId owner) where T : struct, Vehicle.IVehicleState
        {
            int index = GetFirstEmptyIndex(state.Type, owner);
            if (index > -1)
            {
                UpdateVehicle(state);
                return true;
            }
            return false;
        }

        public bool SpawnGlobal<T>(T state, PlayerId owner) where T : struct, Vehicle.IVehicleState
        {
            int index = GetFirstEmptyIndex(state.Type, owner);
            if (index > -1)
            {
                state.ArrayIndex = index;

                Map.Instance.ReliableSender.Add(state);
                Map.Instance.ReliableSender.Send();

                return true;
            }
            return false;
        }
    }
}
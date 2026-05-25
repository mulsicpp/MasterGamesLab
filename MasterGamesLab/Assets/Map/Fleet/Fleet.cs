using Map.Infrastructure;
using System;
using System.Collections.Generic;

namespace Map.Fleet
{
    public class Fleet : IReadOnlyFleet
    {
        private Truck[] trucks;
        public IReadOnlyList<Truck> Trucks => trucks;

        public Fleet()
        {
            trucks = new Truck[Constants.MAX_TRUCK_COUNT];
            for (int i = 0; i < trucks.Length; i++) trucks[i] = new Truck(new VehicleIndex((byte)i));
        }

        public int GetFirstEmptyIndex(Vehicle.VehicleType type)
        {
            Vehicle[] vehicles = null;

            switch (type)
            {
                case Vehicle.VehicleType.Truck: vehicles = trucks; break;
            }

            if (vehicles == null) return -1;

            for (int i = 0; i < vehicles.Length; i++)
            {
                if (!vehicles[i].Exists)
                    return i;
            }
            return -1;
        }

        public void UpdateVehicle<T>(T state) where T : struct, Vehicle.IVehicleState
        {
            if (state is Truck.TruckState t) trucks[t.ArrayIndex].State = t;
            else throw new ArgumentException("Given IVehicleState is not supported: " + state.GetType().FullName);
        }

        public bool SpawnLocal<T>(T state) where T : struct, Vehicle.IVehicleState
        {
            int index = GetFirstEmptyIndex(state.Type);
            if (index > -1)
            {
                UpdateVehicle(state);
                return true;
            }
            return false;
        }

        public bool SpawnGlobal<T>(T state) where T : struct, Vehicle.IVehicleState
        {
            int index = GetFirstEmptyIndex(state.Type);
            if (index > -1)
            {
                state.ArrayIndex = index;

                var nextTimestamp = Map.Instance.Timestamp.Next();
                Map.Instance.UpdateGenericStatesClient(nextTimestamp, new[] { state });
                return true;
            }
            return false;
        }
    }
}
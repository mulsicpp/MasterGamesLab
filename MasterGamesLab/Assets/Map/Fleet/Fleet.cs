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

        public int GetFirstAvailableStructureOffset(Vehicle.VehicleType type)
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
    }
}
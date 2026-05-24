using System.Collections.Generic;

namespace Map.Fleet
{
    public interface IReadOnlyFleet
    {
        public IReadOnlyList<Truck> Trucks { get; }
    }
}
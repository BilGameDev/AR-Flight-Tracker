using System.Collections.Generic;

namespace FlightTracker.Data
{
    public class FlightData
    {
        public long Time { get; set; }
        public List<AircraftState> States { get; set; } = new();
    }
}

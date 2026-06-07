using System;
using System.Collections.Generic;
using System.Linq;
using FlightTracker.Data;

namespace FlightTracker.Utilities
{
    public class FlightDataCache
    {
        private readonly Dictionary<string, AircraftState> cache = new();
        private DateTime lastUpdated;
        private readonly float maxAgeSeconds;

        public int Count => cache.Count;
        public DateTime LastUpdated => lastUpdated;
        public bool IsStale => (DateTime.UtcNow - lastUpdated).TotalSeconds > maxAgeSeconds;
        public IReadOnlyList<AircraftState> AllStates => cache.Values.ToList();

        public FlightDataCache(float maxAgeSeconds = 30f)
        {
            this.maxAgeSeconds = maxAgeSeconds;
        }

        public void Update(List<AircraftState> states)
        {
            cache.Clear();
            foreach (var state in states)
            {
                cache[state.Icao24] = state;
            }
            lastUpdated = DateTime.UtcNow;
        }

        public bool TryGet(string icao24, out AircraftState state)
        {
            return cache.TryGetValue(icao24, out state);
        }

        public List<AircraftState> GetFlightsNearby(double lat, double lon, double radiusKm)
        {
            return cache.Values
                .Where(s => s.HasPosition)
                .Where(s => GeoUtils.HaversineDistance(lat, lon, s.Latitude.Value, s.Longitude.Value) <= radiusKm)
                .ToList();
        }

        public void Clear()
        {
            cache.Clear();
        }
    }
}

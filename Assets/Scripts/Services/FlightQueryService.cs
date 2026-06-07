using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlightTracker.Data;
using UnityEngine;

namespace FlightTracker.Services
{
    public class FlightQueryService
    {
        private readonly IOpenSkyService openSky;

        public FlightQueryService(IOpenSkyService openSky)
        {
            this.openSky = openSky;
        }

        public async Task<AircraftState> FindFlightByCallsignAsync(string callsign)
        {
            if (string.IsNullOrWhiteSpace(callsign)) return null;

            var results = await openSky.GetStatesByCallsignAsync(callsign.Trim());
            return results.Count > 0 ? results[0] : null;
        }

        public async Task<List<AircraftState>> FindFlightsByAirlinePrefixAsync(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return new List<AircraftState>();

            var allStates = await openSky.GetAllStatesAsync();
            return allStates.FindAll(s =>
                !string.IsNullOrEmpty(s.Callsign) &&
                s.Callsign.Trim().StartsWith(prefix.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<AircraftState>> FindFlightsNearbyAsync(double lat, double lon, double radiusKm = 100)
        {
            var bounds = FlightBounds.FromCenterAndRadius(lat, lon, radiusKm);
            return await openSky.GetStatesInAreaAsync(bounds);
        }

        public async Task<List<AircraftState>> FindHighAltitudeFlightsAsync(double minAltMeters = 6000)
        {
            var allStates = await openSky.GetAllStatesAsync();
            return allStates.FindAll(s => s.Altitude.HasValue && s.Altitude.Value >= minAltMeters);
        }
    }
}

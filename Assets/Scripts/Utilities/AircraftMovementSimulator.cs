using System.Collections.Generic;
using FlightTracker.Data;
using UnityEngine;

namespace FlightTracker.Utilities
{
    public class AircraftMovementSimulator
    {
        private class SimulatedAircraft
        {
            public AircraftState State;
            public double LastLatitude;
            public double LastLongitude;
            public double LastAltitude;
            public double SimulatedLatitude;
            public double SimulatedLongitude;
            public double SimulatedAltitude;
            public double Timestamp;
        }

        private readonly Dictionary<string, SimulatedAircraft> aircraft = new();
        private readonly List<AircraftState> simulatedStates = new();
        private double currentTime;

        public IReadOnlyList<AircraftState> SimulatedStates => simulatedStates;

        public void UpdateData(List<AircraftState> newStates)
        {
            currentTime = Time.timeAsDouble;

            var updated = new HashSet<string>();

            foreach (var state in newStates)
            {
                if (!state.HasPosition) continue;

                updated.Add(state.Icao24);

                if (aircraft.TryGetValue(state.Icao24, out var sim))
                {
                    sim.State = state;
                    sim.LastLatitude = sim.SimulatedLatitude;
                    sim.LastLongitude = sim.SimulatedLongitude;
                    sim.LastAltitude = sim.SimulatedAltitude;
                    sim.SimulatedLatitude = state.Latitude.Value;
                    sim.SimulatedLongitude = state.Longitude.Value;
                    sim.SimulatedAltitude = state.Altitude.GetValueOrDefault(0);
                    sim.Timestamp = currentTime;
                }
                else
                {
                    aircraft[state.Icao24] = new SimulatedAircraft
                    {
                        State = state,
                        LastLatitude = state.Latitude.Value,
                        LastLongitude = state.Longitude.Value,
                        LastAltitude = state.Altitude.GetValueOrDefault(0),
                        SimulatedLatitude = state.Latitude.Value,
                        SimulatedLongitude = state.Longitude.Value,
                        SimulatedAltitude = state.Altitude.GetValueOrDefault(0),
                        Timestamp = currentTime
                    };
                }
            }

            foreach (var key in aircraft.Keys)
            {
                if (!updated.Contains(key))
                    aircraft.Remove(key);
            }
        }

        public void StepSimulation(float deltaTime)
        {
            currentTime = Time.timeAsDouble;
            simulatedStates.Clear();

            foreach (var kvp in aircraft)
            {
                var sim = kvp.Value;
                var state = sim.State;

                double elapsed = currentTime - sim.Timestamp;

                double velocityMps = state.Velocity.GetValueOrDefault(0);
                double headingDeg = state.Heading.GetValueOrDefault(0);
                double verticalRateMps = state.VerticalRate.GetValueOrDefault(0);

                if (velocityMps > 0.5)
                {
                    double distanceM = velocityMps * elapsed;
                    double distanceKm = distanceM / 1000.0;
                    double headingRad = headingDeg * Mathf.Deg2Rad;

                    double dLat = distanceKm * Mathf.Cos((float)headingRad) / 111.32;
                    double dLon = distanceKm * Mathf.Sin((float)headingRad) / (111.32 * Mathf.Cos((float)(sim.LastLatitude * Mathf.Deg2Rad)));

                    sim.SimulatedLatitude = sim.LastLatitude + dLat;
                    sim.SimulatedLongitude = sim.LastLongitude + dLon;
                }

                if (verticalRateMps != 0)
                {
                    sim.SimulatedAltitude = sim.LastAltitude + verticalRateMps * elapsed;
                }

                var clone = new AircraftState
                {
                    Icao24 = state.Icao24,
                    Callsign = state.Callsign,
                    OriginCountry = state.OriginCountry,
                    TimePosition = state.TimePosition,
                    LastContact = state.LastContact,
                    Longitude = sim.SimulatedLongitude,
                    Latitude = sim.SimulatedLatitude,
                    Altitude = sim.SimulatedAltitude,
                    OnGround = state.OnGround,
                    Velocity = state.Velocity,
                    Heading = state.Heading,
                    VerticalRate = state.VerticalRate,
                    GeoAltitude = state.GeoAltitude,
                    Squawk = state.Squawk,
                    Spi = state.Spi,
                    Source = state.Source
                };

                simulatedStates.Add(clone);
            }
        }

        public void Clear()
        {
            aircraft.Clear();
            simulatedStates.Clear();
        }
    }
}

using System;

namespace FlightTracker.Data
{
    public class AircraftState
    {
        public string Icao24 { get; set; }
        public string Callsign { get; set; }
        public string OriginCountry { get; set; }
        public long? TimePosition { get; set; }
        public long LastContact { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public double? Altitude { get; set; }
        public bool OnGround { get; set; }
        public double? Velocity { get; set; }
        public double? Heading { get; set; }
        public double? VerticalRate { get; set; }
        public double? GeoAltitude { get; set; }
        public string Squawk { get; set; }
        public bool Spi { get; set; }
        public PositionSource Source { get; set; }

        public bool HasPosition => Latitude.HasValue && Longitude.HasValue
            && Math.Abs(Latitude.Value) > 0.001 && Math.Abs(Longitude.Value) > 0.001;
        public string DisplayCallsign => string.IsNullOrEmpty(Callsign) ? "N/A" : Callsign.Trim();
        public DateTime LastContactTime => DateTimeOffset.FromUnixTimeSeconds(LastContact).DateTime;

        public enum PositionSource
        {
            ADS_B = 0,
            ASTERIX = 1,
            MLAT = 2,
            FLARM = 3,
            Unknown = 4
        }
    }
}

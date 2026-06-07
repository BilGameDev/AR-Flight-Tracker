namespace FlightTracker.Data
{
    public readonly struct FlightBounds
    {
        public double MinLatitude { get; }
        public double MaxLatitude { get; }
        public double MinLongitude { get; }
        public double MaxLongitude { get; }

        public FlightBounds(double minLat, double maxLat, double minLon, double maxLon)
        {
            MinLatitude = minLat;
            MaxLatitude = maxLat;
            MinLongitude = minLon;
            MaxLongitude = maxLon;
        }

        public bool Contains(double lat, double lon)
        {
            return lat >= MinLatitude && lat <= MaxLatitude
                && lon >= MinLongitude && lon <= MaxLongitude;
        }

        public static FlightBounds FromCenterAndRadius(double centerLat, double centerLon, double radiusKm)
        {
            double latDegree = radiusKm / 111.32;
            double lonDegree = radiusKm / (111.32 * System.Math.Cos(centerLat * System.Math.PI / 180));
            return new FlightBounds(centerLat - latDegree, centerLat + latDegree,
                                     centerLon - lonDegree, centerLon + lonDegree);
        }
    }
}

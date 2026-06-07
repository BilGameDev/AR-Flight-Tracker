using System;
using UnityEngine;

namespace FlightTracker.Utilities
{
    public static class GeoUtils
    {
        private const double EarthRadiusKm = 6371.0;
        private const double MetersPerKm = 1000.0;

        public static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }

        public static double Bearing(double lat1, double lon1, double lat2, double lon2)
        {
            double dLon = ToRadians(lon2 - lon1);
            double y = Math.Sin(dLon) * Math.Cos(ToRadians(lat2));
            double x = Math.Cos(ToRadians(lat1)) * Math.Sin(ToRadians(lat2)) -
                       Math.Sin(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Cos(dLon);
            double bearing = ToDegrees(Math.Atan2(y, x));
            return (bearing + 360) % 360;
        }

        public static Vector3 GeoToUnityPosition(double originLat, double originLon, double originAlt,
                                                   double targetLat, double targetLon, double targetAlt,
                                                   float scale = 1f)
        {
            double distanceKm = HaversineDistance(originLat, originLon, targetLat, targetLon);
            double distanceM = distanceKm * MetersPerKm;
            double bearing = Bearing(originLat, originLon, targetLat, targetLon);
            double bearingRad = ToRadians(bearing);

            float x = (float)(distanceM * Math.Sin(bearingRad)) * scale;
            float z = (float)(distanceM * Math.Cos(bearingRad)) * scale;
            float y = (float)(targetAlt - originAlt) * scale;

            return new Vector3(x, y, z);
        }

        public static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
        public static double ToDegrees(double radians) => radians * 180.0 / Math.PI;

        public static (double lat, double lon) DestinationPoint(double startLat, double startLon, double bearingDeg, double distanceKm)
        {
            double angularDistance = distanceKm / EarthRadiusKm;
            double bearingRad = ToRadians(bearingDeg);
            double latRad = ToRadians(startLat);
            double lonRad = ToRadians(startLon);

            double newLatRad = Math.Asin(
                Math.Sin(latRad) * Math.Cos(angularDistance) +
                Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(bearingRad));

            double newLonRad = lonRad + Math.Atan2(
                Math.Sin(bearingRad) * Math.Sin(angularDistance) * Math.Cos(latRad),
                Math.Cos(angularDistance) - Math.Sin(latRad) * Math.Sin(newLatRad));

            return (ToDegrees(newLatRad), ToDegrees(newLonRad));
        }
    }
}

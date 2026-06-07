using FlightTracker.Utilities;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace FlightTracker.AR
{
    public class GeoAnchorService : MonoBehaviour
    {
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private float worldScale = 1f;

        private double originLatitude;
        private double originLongitude;
        private double originAltitude;
        private bool originSet;

        public bool OriginSet => originSet;
        public double OriginLatitude => originLatitude;
        public double OriginLongitude => originLongitude;
        public double OriginAltitude => originAltitude;
        public float WorldScale => worldScale;

        private void Awake()
        {
            if (xrOrigin == null)
                xrOrigin = FindFirstObjectByType<XROrigin>();
        }

        public void SetOrigin(double latitude, double longitude, double altitude)
        {
            originLatitude = latitude;
            originLongitude = longitude;
            originAltitude = altitude;
            originSet = true;
            Debug.Log($"Geo anchor origin set: {latitude:F4}, {longitude:F4}, alt: {altitude:F1}m");
        }

        public Vector3 GeoToWorld(double targetLat, double targetLon, double targetAlt)
        {
            if (!originSet)
            {
                Debug.LogWarning("Geo origin not set. Returning zero vector.");
                return Vector3.zero;
            }

            return GeoUtils.GeoToUnityPosition(
                originLatitude, originLongitude, originAltitude,
                targetLat, targetLon, targetAlt,
                worldScale
            );
        }

        public float GetDistanceToOrigin(double lat, double lon)
        {
            return (float)(GeoUtils.HaversineDistance(originLatitude, originLongitude, lat, lon) * 1000.0);
        }
    }
}

using FlightTracker.Services;
using UnityEngine;

namespace FlightTracker.Configuration
{
    [CreateAssetMenu(fileName = "FlightTrackerConfig", menuName = "Flight Tracker/Configuration", order = 1)]
    public class FlightTrackerConfig : ScriptableObject
    {
        [Header("Query Settings")]
        [SerializeField] private float refreshIntervalSeconds = 15f;
        [SerializeField] private float searchRadiusKm = 200f;
        [SerializeField] private int maxVisibleAircraft = 500;

        [Header("Scan Mode")]
        [SerializeField] private bool manualScanMode = true;
        [SerializeField] private float scanDistanceKm = 500f;
        [SerializeField] private float scanWidthKm = 500f;

        [Header("Test Location (used when GPS unavailable)")]
        [SerializeField] private bool useTestLocation;
        [SerializeField] private double testLatitude;
        [SerializeField] private double testLongitude;

        [Header("AR Rendering")]
        [SerializeField] private float worldScale = 1f;
        [SerializeField] private float minAltitudeMeters = 100f;
        [SerializeField] private bool showOnlyAirborne = true;
        public float RefreshIntervalSeconds => Mathf.Max(5f, refreshIntervalSeconds);
        public float SearchRadiusKm => Mathf.Clamp(searchRadiusKm, 10f, 500f);
        public int MaxVisibleAircraft => Mathf.Max(1, maxVisibleAircraft);
        public bool UseTestLocation => useTestLocation;
        public double TestLatitude => testLatitude;
        public double TestLongitude => testLongitude;
        public float WorldScale => worldScale;
        public float MinAltitudeMeters => minAltitudeMeters;
        public bool ShowOnlyAirborne => showOnlyAirborne;
        public bool ManualScanMode => manualScanMode;
        public float ScanDistanceKm => Mathf.Max(20f, scanDistanceKm);
        public float ScanWidthKm => Mathf.Max(20f, scanWidthKm);
    }
}

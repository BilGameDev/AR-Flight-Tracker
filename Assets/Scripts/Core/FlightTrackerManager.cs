
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.AR;
using FlightTracker.Configuration;
using FlightTracker.Data;
using FlightTracker.Services;
using FlightTracker.UI;
using FlightTracker.Utilities;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using Viridian.Utils;

namespace FlightTracker.Core
{
    public class FlightTrackerManager : MonoBehaviour
    {
        private FlightTrackerConfig config;
        private IOpenSkyService openSky;
        private IUserLocationService locationService;
        private FlightQueryService queryService;
        private FlightDataCache cache;
        private AircraftMovementSimulator simulator;

        [SerializeField] private AircraftInstanceRenderer instanceRenderer;
        [SerializeField] private AircraftTapHandler tapHandler;
        [SerializeField] private GeoAnchorService geoAnchor;
        [SerializeField] private FlightPointer flightPointer;
        [SerializeField] private FlightCountDisplay countDisplay;
        [SerializeField] private StatusMessage statusMessage;
        [SerializeField] private Button scanButton;
        [SerializeField] private Button searchButton;
        [SerializeField] private Button toggleCameraButton;
        [SerializeField] private Button orbitToggleButton;

        private CancellationTokenSource updateCts;
        private ARCameraBackground arCameraBackground;
        private TrackedPoseDriver trackedPoseDriver;
        private OrbitCameraController orbitController;
        private bool isInitialized;
        private double effectiveLatitude;
        private double effectiveLongitude;
        private FlightDetailsUIPopup detailsPopup;
        private FlightSearchUIPopup searchPopup;
        private bool isScanning;

        private void Awake()
        {
            AppContext.Register(this);

            scanButton.onClick.AddListener(OnScanButtonClicked);
            searchButton.onClick.AddListener(OnSearchButtonClicked);
            toggleCameraButton.onClick.AddListener(OnToggleCameraClicked);
            orbitToggleButton.onClick.AddListener(OnOrbitToggleClicked);

            config = AppContext.Get<FlightTrackerConfig>();
            openSky = AppContext.Get<IOpenSkyService>();
            locationService = AppContext.Get<IUserLocationService>();
            queryService = AppContext.Get<FlightQueryService>();
            cache = AppContext.Get<FlightDataCache>();
            simulator = AppContext.Get<AircraftMovementSimulator>();

            instanceRenderer = instanceRenderer ? instanceRenderer : FindFirstObjectByType<AircraftInstanceRenderer>();
            AppContext.Register(instanceRenderer);

            tapHandler = tapHandler ? tapHandler : FindFirstObjectByType<AircraftTapHandler>();
            geoAnchor = geoAnchor ? geoAnchor : FindFirstObjectByType<GeoAnchorService>();
            flightPointer = flightPointer ? flightPointer : AppContext.Get<FlightPointer>();
            countDisplay = countDisplay ? countDisplay : FindFirstObjectByType<FlightCountDisplay>();
            statusMessage = statusMessage ? statusMessage : FindFirstObjectByType<StatusMessage>();

            arCameraBackground = FindFirstObjectByType<ARCameraBackground>();
            orbitController = FindFirstObjectByType<OrbitCameraController>();
            if (orbitController == null)
                orbitController = gameObject.AddComponent<OrbitCameraController>();
            trackedPoseDriver = FindFirstObjectByType<TrackedPoseDriver>();

            locationService.OnPermissionDenied += () => ShowStatus("Location access failed. Check permissions and GPS.");
        }

        void OnToggleCameraClicked()
        {
            if (arCameraBackground == null) return;
            if (orbitController != null && orbitController.IsOrbiting) return;
            arCameraBackground.enabled = !arCameraBackground.enabled;
            ShowStatus(arCameraBackground.enabled ? "Camera background ON" : "Camera background OFF");
        }

        void OnOrbitToggleClicked()
        {
            if (orbitController == null) return;
            trackedPoseDriver.enabled = orbitController.IsOrbiting;
            toggleCameraButton.interactable = orbitController.IsOrbiting;

            if (orbitController.IsOrbiting)
            {
                orbitController.DisableOrbit();
                ShowStatus("Orbit OFF");
            }
            else
            {
                orbitController.EnableOrbit();
                ShowStatus("Orbit ON — swipe to rotate");
            }
        }

        private async void Start()
        {
            ShowStatus("Initializing location services...");
            locationService.StartLocationService();

            int waitFrames = 0;
            while (!locationService.IsInitialized && waitFrames < 200)
            {
                await Task.Yield();
                waitFrames++;
            }

            double lat = locationService.Latitude;
            double lon = locationService.Longitude;

            if (!locationService.IsInitialized || (System.Math.Abs(lat) < 0.1 && System.Math.Abs(lon) < 0.1))
            {
                if (config.UseTestLocation)
                {
                    lat = config.TestLatitude;
                    lon = config.TestLongitude;
                    ShowStatus($"GPS unavailable. Using test location: {lat:F2}, {lon:F2}");
                }
                else
                {
                    ShowStatus("GPS unavailable. Enable test location in config.");
                    return;
                }
            }
            else
            {
                ShowStatus($"GPS connected: {lat:F2}, {lon:F2}");
            }

            effectiveLatitude = lat;
            effectiveLongitude = lon;
            geoAnchor.SetOrigin(lat, lon, locationService.Altitude);

            isInitialized = true;

            updateCts = new CancellationTokenSource();
            _ = StartUpdateLoop(updateCts.Token);
        }

        private void Update()
        {
            if (!isInitialized) return;

            simulator.StepSimulation(Time.deltaTime);
            var simulated = simulator.SimulatedStates;
            Vector3 userPos = Camera.main != null ? Camera.main.transform.position : geoAnchor.transform.position;

            if (simulated.Count == 0)
            {
                instanceRenderer.Clear();
                return;
            }

            instanceRenderer.UpdateInstances(new List<AircraftState>(simulated), userPos,
                effectiveLatitude, effectiveLongitude, geoAnchor.OriginAltitude);
        }

        void OnScanButtonClicked()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 fwd = cam.transform.forward;
            Vector3 flatFwd = new Vector3(fwd.x, 0, fwd.z);
            if (flatFwd.sqrMagnitude < 0.001f) flatFwd = Vector3.forward;

            float bearing = Mathf.Atan2(flatFwd.x, flatFwd.z) * Mathf.Rad2Deg;
            if (bearing < 0) bearing += 360f;

            ScanDirection(bearing);
        }

        void OnSearchButtonClicked()
        {
            OpenSearch();
        }

        public async void ScanDirection(float bearingDeg)
        {
            if (!isInitialized || isScanning) return;
            isScanning = true;

            ShowStatus($"Scanning at {bearingDeg:F0}°...");

            double scanDist = config.ScanDistanceKm;
            double halfW = config.ScanWidthKm * 0.5;

            var (fLat, fLon) = GeoUtils.DestinationPoint(
                effectiveLatitude, effectiveLongitude, bearingDeg, scanDist);

            var (flLat, flLon) = GeoUtils.DestinationPoint(
                fLat, fLon, (bearingDeg + 270) % 360, halfW);
            var (frLat, frLon) = GeoUtils.DestinationPoint(
                fLat, fLon, (bearingDeg + 90) % 360, halfW);

            var (nlLat, nlLon) = GeoUtils.DestinationPoint(
                effectiveLatitude, effectiveLongitude, (bearingDeg + 270) % 360, halfW);
            var (nrLat, nrLon) = GeoUtils.DestinationPoint(
                effectiveLatitude, effectiveLongitude, (bearingDeg + 90) % 360, halfW);

            double minLat = Mathf.Min((float)flLat, (float)frLat, (float)nlLat, (float)nrLat);
            double maxLat = Mathf.Max((float)flLat, (float)frLat, (float)nlLat, (float)nrLat);
            double minLon = Mathf.Min((float)flLon, (float)frLon, (float)nlLon, (float)nrLon);
            double maxLon = Mathf.Max((float)flLon, (float)frLon, (float)nlLon, (float)nrLon);

            var bounds = new FlightBounds(minLat, maxLat, minLon, maxLon);
            simulator.Clear();

            try
            {
                var states = await openSky.GetStatesInAreaAsync(bounds);
                if (states == null || states.Count == 0)
                {
                    ShowStatus("No aircraft found in that direction.");
                    return;
                }

                if (config.ShowOnlyAirborne)
                {
                    states = states.FindAll(s => !s.OnGround && s.Altitude.GetValueOrDefault(0) >= config.MinAltitudeMeters);
                }

                if (config.MaxVisibleAircraft > 0 && states.Count > config.MaxVisibleAircraft)
                {
                    states = states.GetRange(0, config.MaxVisibleAircraft);
                }

                simulator.UpdateData(states);
                countDisplay?.UpdateCount(states.Count);
                ShowStatus($"Scan found {states.Count} aircraft.");
            }
            catch (System.Exception e)
            {
                ShowStatus($"Scan failed: {e.Message}");
            }
            finally
            {
                isScanning = false;
            }
        }

        private async Task StartUpdateLoop(CancellationToken token)
        {
            if (!config.ManualScanMode)
            {
                while (!token.IsCancellationRequested)
                {
                    if (isInitialized)
                    {
                        await RefreshFlightData(token);
                    }

                    try
                    {
                        await Task.Delay((int)(config.RefreshIntervalSeconds * 1000), token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
            else
            {
                try
                {
                    await Task.Delay(-1, token);
                }
                catch (TaskCanceledException) { }
            }
        }

        private async Task RefreshFlightData(CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            var bounds = FlightBounds.FromCenterAndRadius(
                effectiveLatitude, effectiveLongitude, config.SearchRadiusKm);

            List<AircraftState> states = null;

            try
            {
                states = await openSky.GetStatesInAreaAsync(bounds);
                if (token.IsCancellationRequested) return;

                if (states == null || states.Count == 0)
                {
                    Debug.Log("[FlightTracker] No aircraft data received from OpenSky.");
                    return;
                }

                if (config.ShowOnlyAirborne)
                {
                    states = states.FindAll(s => !s.OnGround && s.Altitude.GetValueOrDefault(0) >= config.MinAltitudeMeters);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FlightTracker] Failed to refresh flight data: {e.Message}");
                if (cache.Count > 0)
                {
                    Debug.Log($"[FlightTracker] Using cached data ({cache.Count} aircraft).");
                    states = new List<AircraftState>(cache.AllStates);
                }
            }

            if (states == null || states.Count == 0) return;

            if (config.MaxVisibleAircraft > 0 && states.Count > config.MaxVisibleAircraft)
            {
                states = states.GetRange(0, config.MaxVisibleAircraft);
            }

            simulator.UpdateData(states);
            countDisplay?.UpdateCount(states.Count);
            Debug.Log($"[FlightTracker] Updated {states.Count} aircraft positions.");
        }

        private void OnEnable()
        {
            if (tapHandler != null)
                tapHandler.OnAircraftTapped += OnAircraftTapped;
        }

        private void OnDisable()
        {
            if (tapHandler != null)
                tapHandler.OnAircraftTapped -= OnAircraftTapped;
        }

        private void OnAircraftTapped(AircraftState flight)
        {
            ShowDetailsPopup(flight);
            ShowStatus($"Selected: {flight.DisplayCallsign}");
        }

        private void OnEmptyTapped() { }

        public async void OnSearchRequested(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 3)
            {
                searchPopup?.ClearResults();
                return;
            }

            query = query.Trim();
            searchPopup?.SetStatus($"Searching for \"{query}\"...");
            var results = await queryService.FindFlightsByAirlinePrefixAsync(query);

            searchPopup?.DisplayResults(results);
        }

        public void OpenSearch()
        {
            searchPopup = FlightSearchUIPopup.Show();
            if (searchPopup == null) return;

            searchPopup.OnSearchRequested += OnSearchRequested;
            searchPopup.OnFlightSelected += flight =>
            {
                searchPopup.Close();
                searchPopup = null;
                ShowDetailsPopup(flight);
            };
        }

        private void ShowDetailsPopup(AircraftState flight)
        {
            instanceRenderer.SetSelected(flight.Icao24);

            if (detailsPopup != null)
            {
                detailsPopup.UpdateDetails(flight);
            }
            else
            {
                detailsPopup = FlightDetailsUIPopup.Show(flight);
                if (detailsPopup != null)
                {
                    detailsPopup.OnFlightChanged += OnDetailsFlightChanged;
                    detailsPopup.OnClose += OnDetailsPopupClosed;
                }
            }

            Vector3? fpPos = instanceRenderer.GetWorldPosition(flight.Icao24);
            if (fpPos.HasValue)
                flightPointer?.ShowPointer(flight, fpPos.Value);
            else
                flightPointer?.HidePointer();
        }

        private void OnDetailsFlightChanged(AircraftState flight)
        {
            instanceRenderer.SetSelected(flight.Icao24);
            Vector3? fpPos = instanceRenderer.GetWorldPosition(flight.Icao24);
            if (fpPos.HasValue)
                flightPointer?.ShowPointer(flight, fpPos.Value);
            else
                flightPointer?.HidePointer();
        }

        private void OnDetailsPopupClosed()
        {
            instanceRenderer.ClearSelection();
            flightPointer?.HidePointer();
            detailsPopup = null;
        }

        private void ShowStatus(string message)
        {
            statusMessage?.Show(message);
            Debug.Log($"[FlightTracker] {message}");
        }

        private void OnDestroy()
        {
            updateCts?.Cancel();
            updateCts?.Dispose();

            searchButton.onClick.RemoveListener(OnSearchButtonClicked);
            scanButton.onClick.RemoveListener(OnScanButtonClicked);
            toggleCameraButton.onClick.RemoveListener(OnToggleCameraClicked);
            orbitToggleButton.onClick.RemoveListener(OnOrbitToggleClicked);
            locationService?.StopLocationService();
        }
    }
}

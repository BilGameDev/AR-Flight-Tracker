using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace FlightTracker.Services
{
    public class UserLocationService : IUserLocationService
    {
        public event Action OnLocationUpdated;
        public event Action OnPermissionDenied;

        public bool IsInitialized { get; private set; }
        public bool PermissionGranted { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double Altitude { get; private set; }
        public float HorizontalAccuracy { get; private set; }

        private MonoBehaviour coroutineHost;
        private bool isRunning;

        public UserLocationService(MonoBehaviour coroutineHost)
        {
            this.coroutineHost = coroutineHost;
        }

        public void StartLocationService()
        {
            if (isRunning) return;
            isRunning = true;
            coroutineHost.StartCoroutine(InitializeLocation());
        }

        public void StopLocationService()
        {
            isRunning = false;
            Input.location.Stop();
        }

        private IEnumerator InitializeLocation()
        {
#if UNITY_ANDROID
            yield return RequestLocationPermission();
            if (!PermissionGranted)
            {
                Debug.LogWarning("Location permission denied by user.");
                OnPermissionDenied?.Invoke();
                isRunning = false;
                yield break;
            }
#endif

            Input.location.Start(10f, 1f);

            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1f);
                maxWait--;
            }

            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.LogError("GPS initialization failed. Location may be disabled on device.");
                OnPermissionDenied?.Invoke();
                isRunning = false;
                yield break;
            }

            PermissionGranted = true;
            IsInitialized = true;
            UpdateLocation();
            OnLocationUpdated?.Invoke();

            coroutineHost.StartCoroutine(PollLocation());
        }

#if UNITY_ANDROID
        private IEnumerator RequestLocationPermission()
        {
            const string permission = "android.permission.ACCESS_FINE_LOCATION";

            if (Permission.HasUserAuthorizedPermission(permission))
            {
                PermissionGranted = true;
                yield break;
            }

            bool done = false;
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => { PermissionGranted = true; done = true; };
            callbacks.PermissionDenied += _ => { PermissionGranted = false; done = true; };
            callbacks.PermissionDeniedAndDontAskAgain += _ => { PermissionGranted = false; done = true; };

            Permission.RequestUserPermission(permission, callbacks);

            float timeout = 30f;
            while (!done && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (!done)
            {
                Debug.LogWarning("Location permission request timed out.");
                PermissionGranted = false;
            }
        }
#endif

        private IEnumerator PollLocation()
        {
            while (isRunning)
            {
                yield return new WaitForSeconds(5f);
                UpdateLocation();
                OnLocationUpdated?.Invoke();
            }
        }

        private void UpdateLocation()
        {
            var loc = Input.location.lastData;
            Latitude = loc.latitude;
            Longitude = loc.longitude;
            Altitude = loc.altitude;
            HorizontalAccuracy = loc.horizontalAccuracy;
        }
    }
}

using FlightTracker.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using Viridian.Utils;

namespace FlightTracker.UI
{
    public class ScanController : MonoBehaviour
    {
        private Camera arCamera;
        private UIEvents uiEvents;
        private FlightTrackerManager manager;

        private void Awake()
        {
            uiEvents = AppContext.Get<IUIEvents>() as UIEvents;
            if (uiEvents != null)
                uiEvents.OnScanRequested += HandleScanRequest;
        }

        void OnDestroy()
        {
            if (uiEvents != null)
                uiEvents.OnScanRequested -= HandleScanRequest;
        }

        private void Start()
        {
            arCamera = Camera.main;
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                HandleScanRequest();
#endif
        }

        void HandleScanRequest()
        {
            if (manager == null) manager = AppContext.Get<FlightTrackerManager>();
            if (manager == null || arCamera == null) return;

            Vector3 fwd = arCamera.transform.forward;
            Vector3 flatFwd = new Vector3(fwd.x, 0, fwd.z);
            if (flatFwd.sqrMagnitude < 0.001f) flatFwd = Vector3.forward;

            float bearing = Mathf.Atan2(flatFwd.x, flatFwd.z) * Mathf.Rad2Deg;
            if (bearing < 0) bearing += 360f;

            manager.ScanDirection(bearing);
        }
    }
}

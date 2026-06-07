using FlightTracker.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
using Viridian.Utils;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace FlightTracker.AR
{
    public class AircraftTapHandler : MonoBehaviour
    {
        [SerializeField] private float tapRadius = 50f;
        private Camera arCamera;
        private AircraftInstanceRenderer instanceRenderer;
    
        public System.Action<AircraftState> OnAircraftTapped;
        public System.Action OnEmptyTapped;

        private void Awake()
        {
            arCamera = Camera.main;
            instanceRenderer = AppContext.Get<AircraftInstanceRenderer>()
                ?? FindFirstObjectByType<AircraftInstanceRenderer>();
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
            TouchSimulation.Enable();
#endif
            Touch.onFingerDown += OnFingerDown;
        }

        private void OnDisable()
        {
            Touch.onFingerDown -= OnFingerDown;
        }

        private void OnFingerDown(Finger finger)
        {
            if (arCamera == null) arCamera = Camera.main;

            Vector2 screenPos = finger.screenPosition;
            if (float.IsInfinity(screenPos.x) || float.IsNaN(screenPos.x)) return;
            if (screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height) return;

            if (IsOverUI(screenPos))
                return;

            HandleTap(screenPos);
        }

        private bool IsOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;

            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            return results.Count > 0;
        }

        private void HandleTap(Vector2 screenPos)
        {
            if (arCamera == null) arCamera = Camera.main;
            if (arCamera == null) return;

            Ray ray = arCamera.ScreenPointToRay(screenPos);

            var flights = instanceRenderer != null
                ? instanceRenderer.VisibleFlights
                : null;

            if (flights == null || flights.Count == 0)
            {
                OnEmptyTapped?.Invoke();
                return;
            }

            var flightPositions = instanceRenderer.FlightWorldPositions;

            AircraftState closest = null;
            float closestDist = tapRadius;

            for (int i = 0; i < flights.Count; i++)
            {
                if (i >= flightPositions.Count) break;

                Vector3 flightPos = flightPositions[i];
                float dist = Vector3.Cross(ray.direction, flightPos - ray.origin).magnitude;

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = flights[i];
                }
            }

            if (closest != null)
            {
                OnAircraftTapped?.Invoke(closest);
            }
            else
            {
                OnEmptyTapped?.Invoke();
            }
        }
    }
}

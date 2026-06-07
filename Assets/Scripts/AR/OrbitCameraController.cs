using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

namespace FlightTracker.AR
{
    public class OrbitCameraController : MonoBehaviour
    {
        [SerializeField] private float orbitDistance = 30f;
        [SerializeField] private float sensitivity = 0.5f;

        private Transform camOffsetTransform;
        private ARCameraManager camManager;
        private ARCameraBackground camBackground;

        private Vector3 orbitCenter;
        private float yaw;
        private float pitch;
        private bool isOrbiting;
        private bool wasPressed;
        private Vector2 lastPointerPos;

        public bool IsOrbiting => isOrbiting;

        private void Awake()
        {
            var cam = Camera.main;
            if (cam != null)
                camOffsetTransform = cam.transform;
            camManager = FindFirstObjectByType<ARCameraManager>();
            camBackground = FindFirstObjectByType<ARCameraBackground>();
        }

        public void EnableOrbit()
        {
            if (camOffsetTransform == null) return;

            Vector3 pos = camOffsetTransform.position;
            Vector3 fwd = camOffsetTransform.forward;
            if (float.IsNaN(fwd.x)) fwd = Vector3.forward;

            if (camManager != null) camManager.enabled = false;
            if (camBackground != null) camBackground.enabled = false;

            orbitCenter = pos + fwd * orbitDistance;
            Vector3 offset = pos - orbitCenter;
            yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            float sinVal = Mathf.Clamp(offset.y / offset.magnitude, -1f, 1f);
            pitch = Mathf.Asin(sinVal) * Mathf.Rad2Deg;

            wasPressed = false;
            ApplyOrbit();
            isOrbiting = true;
        }

        public void DisableOrbit()
        {
            isOrbiting = false;
            wasPressed = false;
            if (camManager != null) camManager.enabled = true;
            if (camBackground != null) camBackground.enabled = true;
        }

        private void LateUpdate()
        {
            if (!isOrbiting) return;

            bool pressed = false;
            Vector2 pos = Vector2.zero;

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                var phase = touch.phase.ReadValue();
                if (phase != UnityEngine.InputSystem.TouchPhase.None && phase != UnityEngine.InputSystem.TouchPhase.Ended && phase != UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    pressed = true;
                    pos = touch.position.ReadValue();
                }
            }

#if UNITY_EDITOR
            if (!pressed)
            {
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.isPressed)
                {
                    pressed = true;
                    pos = mouse.position.ReadValue();
                }
            }
#endif

            if (pressed)
            {
                if (!wasPressed)
                {
                    wasPressed = true;
                    lastPointerPos = pos;
                }
                else
                {
                    Vector2 delta = pos - lastPointerPos;
                    lastPointerPos = pos;
                    yaw -= delta.x * sensitivity;
                    pitch = Mathf.Clamp(pitch + delta.y * sensitivity, -89f, 89f);
                }
            }
            else
            {
                wasPressed = false;
            }

            ApplyOrbit();
        }

        private void ApplyOrbit()
        {
            float radPitch = pitch * Mathf.Deg2Rad;
            float radYaw = yaw * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                orbitDistance * Mathf.Cos(radPitch) * Mathf.Sin(radYaw),
                orbitDistance * Mathf.Sin(radPitch),
                orbitDistance * Mathf.Cos(radPitch) * Mathf.Cos(radYaw)
            );
            camOffsetTransform.position = orbitCenter + offset;
            camOffsetTransform.LookAt(orbitCenter);
        }
    }
}
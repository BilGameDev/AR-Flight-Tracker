using FlightTracker.Data;
using UnityEngine;
using UnityEngine.UI;
using Viridian.Utils;

namespace FlightTracker.AR
{
    public class FlightPointer : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Graphic arrowImage;
        [SerializeField] private float edgeMargin = 30f;
        [SerializeField] private float arrowSize = 24f;
        [SerializeField]  private AircraftInstanceRenderer instancedRenderer;

        private AircraftState trackedFlight;
        private bool isActive;
        private Camera cam;
        private RectTransform arrowRt;

        public bool IsActive => isActive;
        public AircraftState TrackedFlight => trackedFlight;


        private void Awake()
        {
            AppContext.Register(this);
            cam = Camera.main;

            arrowRt = (RectTransform)arrowImage.transform;
            arrowRt.sizeDelta = new Vector2(arrowSize, arrowSize);
            arrowRt.anchorMin = Vector2.zero;
            arrowRt.anchorMax = Vector2.zero;
            arrowRt.pivot = new Vector2(0.5f, 0.5f);

            arrowImage.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!isActive || trackedFlight == null || instancedRenderer == null || cam == null) return;
            UpdateIndicator();
        }

        private void UpdateIndicator()
        {
            Vector3? currentPos = instancedRenderer.GetWorldPosition(trackedFlight.Icao24);
            if (currentPos == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(currentPos.Value);

            bool behind = screenPos.z < 0;
            if (behind)
            {
                screenPos.x = Screen.width - screenPos.x;
                screenPos.y = Screen.height - screenPos.y;
            }

            float halfW = Screen.width * 0.5f;
            float halfH = Screen.height * 0.5f;
            Vector2 dir = ((Vector2)screenPos - new Vector2(halfW, halfH)).normalized;

            float margin = edgeMargin + (behind ? 60f : 0f);
            float maxX = halfW - margin;
            float maxY = halfH - margin;

            float scale = Mathf.Min(
                Mathf.Abs(maxX / dir.x),
                Mathf.Abs(maxY / dir.y));

            Vector2 edgePos = new Vector2(halfW, halfH) + dir * scale;

            Rect onScreenRect = new Rect(edgeMargin, edgeMargin,
                Screen.width - edgeMargin * 2, Screen.height - edgeMargin * 2);

            if (onScreenRect.Contains(screenPos))
            {
                arrowImage.gameObject.SetActive(false);
                return;
            }

            arrowImage.gameObject.SetActive(true);
            arrowRt.anchoredPosition = edgePos;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            arrowRt.localRotation = Quaternion.Euler(0, 0, angle);
        }

        private static Sprite CreateArrowSprite()
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32 clear = Color.clear;
            Color32 white = Color.white;

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = x - size * 0.5f;
                    float cy = y - size * 0.5f;
                    float hw = size * 0.4f;
                    float hh = size * 0.4f;

                    bool fill = false;
                    if (Mathf.Abs(cx) <= hw * 0.3f && cy <= hh && cy >= -hh * 0.3f)
                        fill = true;
                    if (cy >= hh * 0.2f)
                    {
                        float triEdge = (hh - cy) * (hw / hh);
                        if (Mathf.Abs(cx) <= triEdge) fill = true;
                    }
                    tex.SetPixel(x, y, fill ? white : clear);
                }

            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        public void ShowPointer(AircraftState flight, Vector3 worldPos)
        {
            trackedFlight = flight;
            isActive = true;
            arrowImage.gameObject.SetActive(false);
        }

        public void HidePointer()
        {
            isActive = false;
            trackedFlight = null;
            arrowImage.gameObject.SetActive(false);
        }
    }
}

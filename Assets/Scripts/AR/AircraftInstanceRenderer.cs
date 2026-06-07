using System.Collections.Generic;
using FlightTracker.Data;
using FlightTracker.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using Viridian.Utils;

namespace FlightTracker.AR
{
    public class AircraftInstanceRenderer : MonoBehaviour
    {
        private const int MaxPerBatch = 1023;
        private const float MaxDisplayAlt = 12000f;

        [SerializeField] private Material material;
        [SerializeField] private float domeRadius = 30f;
        [SerializeField] private float markerScale = 1f;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color selectedColor = Color.green;

        private readonly List<Matrix4x4[]> batches = new();
        private readonly List<int> batchCounts = new();
        private readonly List<AircraftState> visibleFlights = new();
        private readonly List<Vector3> flightWorldPositions = new();
        private readonly List<float> headings = new();

        private MaterialPropertyBlock propBlock;
        private Mesh mesh;
        private bool hasPoints;
        private string selectedIcao24;

        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        public IReadOnlyList<AircraftState> VisibleFlights => visibleFlights;
        public IReadOnlyList<Vector3> FlightWorldPositions => flightWorldPositions;

        private void Awake()
        {
            AppContext.Register(this);

            mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            if (mesh == null)
                mesh = Resources.GetBuiltinResource<Mesh>("Plane.fbx");

            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (shader == null)
                    shader = Shader.Find("UI/Default");

                if (shader != null)
                {
                    material = new Material(shader)
                    {
                        color = Color.white,
                        enableInstancing = true
                    };
                }
            }
            else
            {
                material.enableInstancing = true;
            }

            propBlock = new MaterialPropertyBlock();
            propBlock.SetColor(ColorId, defaultColor);
        }

        public void UpdateInstances(List<AircraftState> flights, Vector3 userPosition,
            double originLat, double originLon, double originAlt)
        {
            visibleFlights.Clear();
            flightWorldPositions.Clear();
            headings.Clear();

            int valid = 0;

            foreach (var flight in flights)
            {
                if (!flight.HasPosition) continue;

                Vector3 worldPos = GeoUtils.GeoToUnityPosition(
                    originLat, originLon, originAlt,
                    flight.Latitude.Value, flight.Longitude.Value,
                    flight.Altitude.GetValueOrDefault(0), 1f);

                Vector3 delta = worldPos - userPosition;
                Vector3 hDir = Vector3.ProjectOnPlane(delta, Vector3.up).normalized;
                if (hDir.sqrMagnitude < 0.001f) hDir = Vector3.forward;

                float t = Mathf.Clamp01(Mathf.Max(0, delta.y) / MaxDisplayAlt);
                float hRadius = Mathf.Cos(t * Mathf.PI * 0.5f) * domeRadius;
                float vOffset = Mathf.Sin(t * Mathf.PI * 0.5f) * domeRadius;

                flightWorldPositions.Add(userPosition + hDir * hRadius + Vector3.up * vOffset);
                visibleFlights.Add(flight);
                headings.Add((float)flight.Heading.GetValueOrDefault(0));
                valid++;
            }

            if (valid == 0)
            {
                Clear();
                return;
            }

            int required = Mathf.CeilToInt(valid / (float)MaxPerBatch);
            while (batches.Count < required)
            {
                batches.Add(new Matrix4x4[MaxPerBatch]);
                batchCounts.Add(0);
            }

            Vector3 scale = Vector3.one * markerScale;
            int idx = 0;

            for (int b = 0; b < batches.Count; b++)
            {
                Matrix4x4[] batch = batches[b];
                int count = Mathf.Min(MaxPerBatch, valid - idx);
                batchCounts[b] = count;

                for (int i = 0; i < count; i++)
                {
                    Vector3 p = flightWorldPositions[idx++];
                    batch[i].SetTRS(p, Quaternion.identity, scale);
                }
            }

            hasPoints = true;
        }

        private void LateUpdate()
        {
            if (!hasPoints || mesh == null || material == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            int hIdx = 0, fIdx = 0;
            Vector3? selPos = null;
            Quaternion? selRot = null;

            propBlock.SetColor(ColorId, defaultColor);

            for (int b = 0; b < batches.Count; b++)
            {
                Matrix4x4[] batch = batches[b];
                int count = batchCounts[b];
                if (count <= 0) continue;

                for (int i = 0; i < count; i++)
                {
                    Vector3 pos = batch[i].GetColumn(3);
                    float deg = hIdx < headings.Count ? headings[hIdx] : 0f;
                    hIdx++;

                    Vector3 toCam = (cam.transform.position - pos).normalized;
                    float rad = deg * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
                    Vector3 up = Vector3.ProjectOnPlane(dir, toCam);
                    if (up.sqrMagnitude < 0.0001f) up = Vector3.up;

                    Quaternion rot = Quaternion.LookRotation(toCam, up);

                    bool isSelected = fIdx < visibleFlights.Count && visibleFlights[fIdx].Icao24 == selectedIcao24;
                    if (isSelected)
                    {
                        selPos = pos;
                        selRot = rot;
                        batch[i].SetTRS(Vector3.one * 99999f, Quaternion.identity, Vector3.zero);
                    }
                    else
                    {
                        batch[i].SetTRS(pos, rot, Vector3.one * markerScale);
                    }
                    fIdx++;
                }

                Graphics.DrawMeshInstanced(mesh, 0, material, batch, count, propBlock,
                    ShadowCastingMode.Off, false, gameObject.layer, null, LightProbeUsage.Off);
            }

            if (selPos.HasValue)
            {
                propBlock.SetColor(ColorId, selectedColor);
                Graphics.DrawMesh(mesh,
                    Matrix4x4.TRS(selPos.Value + Vector3.up * 0.1f, selRot.Value, Vector3.one * markerScale * 1.5f),
                    material, gameObject.layer, null, 0, propBlock);
                propBlock.SetColor(ColorId, defaultColor);
            }
        }

        public Vector3? GetWorldPosition(string icao24)
        {
            for (int i = 0; i < visibleFlights.Count; i++)
            {
                if (visibleFlights[i].Icao24 == icao24)
                    return flightWorldPositions[i];
            }
            return null;
        }

        public void SetSelected(string icao24) => selectedIcao24 = icao24;
        public void ClearSelection() => selectedIcao24 = null;

        public void Clear()
        {
            hasPoints = false;
            for (int i = 0; i < batchCounts.Count; i++)
                batchCounts[i] = 0;
        }

        private void OnDrawGizmos()
        {
            if (flightWorldPositions == null || flightWorldPositions.Count == 0) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < Mathf.Min(flightWorldPositions.Count, 500); i++)
                Gizmos.DrawSphere(flightWorldPositions[i], markerScale * 0.5f);

            Gizmos.color = new Color(0, 1, 0, 0.15f);
            Gizmos.DrawWireSphere(transform.position, domeRadius);
        }
    }
}

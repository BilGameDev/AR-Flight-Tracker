using UnityEngine;

namespace Viridian.Utils
{
    public static class PastelColors
    {
        public static readonly Color[] Palette = new[]
        {
            new Color(0.95f, 0.72f, 0.72f),  // light pink
            new Color(0.72f, 0.90f, 0.95f),  // light blue
            new Color(0.85f, 0.95f, 0.72f),  // light green
            new Color(0.95f, 0.85f, 0.72f),  // light orange
            new Color(0.72f, 0.78f, 0.95f),  // periwinkle
            new Color(0.95f, 0.72f, 0.90f),  // light magenta
            new Color(0.72f, 0.95f, 0.85f),  // mint
            new Color(0.95f, 0.90f, 0.72f),  // light yellow
            new Color(0.82f, 0.72f, 0.95f),  // light purple
            new Color(0.72f, 0.95f, 0.78f),  // light spring green
        };

        public static readonly Color[] Distinct = new[]
        {
            new Color(0.85f, 0.25f, 0.25f),  // red
            new Color(0.20f, 0.55f, 0.85f),  // blue
            new Color(0.25f, 0.70f, 0.30f),  // green
            new Color(0.90f, 0.60f, 0.10f),  // orange
            new Color(0.50f, 0.30f, 0.85f),  // purple
            new Color(0.85f, 0.20f, 0.60f),  // magenta
            new Color(0.10f, 0.75f, 0.75f),  // teal
            new Color(0.85f, 0.75f, 0.15f),  // yellow
            new Color(0.60f, 0.35f, 0.20f),  // brown
            new Color(0.20f, 0.80f, 0.60f),  // emerald
        };

        public static Color Get(int index) => Palette[index % Palette.Length];
        public static Color GetDistinct(int index) => Distinct[index % Distinct.Length];
    }
}
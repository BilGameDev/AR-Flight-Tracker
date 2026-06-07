using UnityEngine;
using UnityEngine.UI;

namespace Viridian.Utils
{
[RequireComponent(typeof(RectTransform))]
public class SafeAreaPadding : MonoBehaviour
{
    [Header("Apply which insets?")]
    public bool padTop = true;
    public bool padBottom = false;
    public bool padLeft = true;
    public bool padRight = true;

    [Tooltip("Extra padding multiplier (1 = exact safe area).")]
    public float multiplier = 1f;

    [Header("Canvas Scaler Match")]
    [SerializeField] private CanvasScaler scaler;
    [SerializeField] private bool _adjustMatch = true;
    [SerializeField] private float _threshold = 1.5f;

    RectTransform rt;
    Rect lastSafeArea;
    Vector2Int lastScreenSize;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        Apply();

        if (_adjustMatch)
        {
            if (scaler != null)
            {
                float aspect = Mathf.Max(Screen.width, Screen.height) / (float)Mathf.Min(Screen.width, Screen.height);
                scaler.matchWidthOrHeight = aspect <= _threshold ? 1f : 0.5f;
            }
        }
    }

    void OnEnable() => Apply();

    void Update()
    {
        if (Screen.safeArea != lastSafeArea ||
            lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
        {
            Apply();
        }
    }

    void Apply()
    {
        var sa = Screen.safeArea;
        lastSafeArea = sa;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        float left   = sa.x;
        float bottom = sa.y;
        float right  = Screen.width  - (sa.x + sa.width);
        float top    = Screen.height - (sa.y + sa.height);

        left   *= multiplier;
        right  *= multiplier;
        top    *= multiplier;
        bottom *= multiplier;

        var offMin = rt.offsetMin;
        var offMax = rt.offsetMax;

        if (padLeft)   offMin.x = left;
        if (padBottom) offMin.y = bottom;
        if (padRight)  offMax.x = -right;
        if (padTop)    offMax.y = -top;

        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
    }
}
}

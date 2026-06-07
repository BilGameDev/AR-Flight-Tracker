using System;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Viridian.Utils
{
public static class LitMotionExtensions
{
    public static MotionHandle TweenFade(this CanvasGroup cg, float to, float duration)
    {
        return LMotion.Create(cg.alpha, to, duration).Bind(x => cg.alpha = x);
    }

    public static MotionHandle TweenColor(this Graphic graphic, Color to, float duration)
    {
        return LMotion.Create(graphic.color, to, duration).Bind(x => graphic.color = x);
    }

    public static MotionHandle TweenFade(this Graphic graphic, float to, float duration)
    {
        var from = graphic.color.a;
        return LMotion.Create(from, to, duration).Bind(x =>
        {
            var c = graphic.color;
            c.a = x;
            graphic.color = c;
        });
    }

    public static MotionHandle TweenColor(this TextMeshProUGUI text, Color to, float duration)
    {
        return LMotion.Create(text.color, to, duration).Bind(x => text.color = x);
    }

    public static MotionHandle TweenFade(this TextMeshProUGUI text, float to, float duration)
    {
        var from = text.color.a;
        return LMotion.Create(from, to, duration).Bind(x =>
        {
            var c = text.color;
            c.a = x;
            text.color = c;
        });
    }

    public static MotionHandle TweenScale(this Transform t, Vector3 to, float duration)
    {
        return LMotion.Create(t.localScale, to, duration).Bind(x => t.localScale = x);
    }

    public static MotionHandle TweenPosition(this Transform t, Vector3 to, float duration)
    {
        return LMotion.Create(t.localPosition, to, duration).Bind(x => t.localPosition = x);
    }

    public static MotionHandle TweenRotation(this Transform t, Quaternion to, float duration)
    {
        return LMotion.Create(t.localRotation, to, duration).Bind(x => t.localRotation = x);
    }

    public static MotionHandle TweenAnchorPos(this RectTransform rt, Vector2 to, float duration)
    {
        return LMotion.Create(rt.anchoredPosition, to, duration).Bind(x => rt.anchoredPosition = x);
    }

    public static MotionHandle TweenSizeDelta(this RectTransform rt, Vector2 to, float duration)
    {
        return LMotion.Create(rt.sizeDelta, to, duration).Bind(x => rt.sizeDelta = x);
    }

    public static MotionHandle TweenDelayedCall(float delay, Action callback)
    {
        return LMotion.Create(0f, 1f, delay).WithOnComplete(callback).RunWithoutBinding();
    }
}
}

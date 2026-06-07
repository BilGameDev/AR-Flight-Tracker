using System;
using System.Collections.Generic;
using LitMotion;
using UnityEngine;

namespace Viridian.Utils
{
public class CanvasFader : MonoBehaviour
{
    public static CanvasFader instance { get; private set; }
    [SerializeField] private bool playOnStart;

    [Header("Fade Settings")]
    [SerializeField] private List<CanvasGroup> targets = new();
    [SerializeField] private float inDuration = 0.5f;
    [SerializeField] private float outDuration = 0.35f;
    [SerializeField] private float staggerDelay = 0.15f;
    [SerializeField] private Ease inEase = Ease.OutCubic;
    [SerializeField] private Ease outEase = Ease.InCubic;
    [SerializeField] private bool startHidden = true;

    [Header("Scale")]
    [SerializeField] private bool useScale;
    [SerializeField] private float inStartScale = 0.92f;
    [SerializeField] private float outEndScale = 0.92f;
    [SerializeField] private Ease scaleInEase = Ease.OutBack;
    [SerializeField] private Ease scaleOutEase = Ease.InCubic;

    private CompositeMotionHandle _motions;

    void Awake()
    {
        instance = this;
        _motions = new CompositeMotionHandle();

        if (targets.Count == 0)
            GetComponentsInChildren(true, targets);

        if (startHidden)
        {
            foreach (var cg in targets)
            {
                if (cg == null) continue;
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
                if (useScale && cg.transform is RectTransform rt)
                    rt.localScale = Vector3.one * inStartScale;
            }
        }
    }

    void Start()
    {
        if (playOnStart)
            PlayIn();
    }

    public void PlayIn()
    {
        _motions.Cancel();
        for (int i = 0; i < targets.Count; i++)
        {
            var cg = targets[i];
            if (cg == null) continue;
            var delay = i * staggerDelay;

            cg.gameObject.SetActive(true);
            cg.blocksRaycasts = false;

            LMotion.Create(cg.alpha, 1f, inDuration)
                .WithDelay(delay)
                .WithEase(inEase)
                .WithOnComplete(() => cg.blocksRaycasts = true)
                .Bind(x => cg.alpha = x)
                .AddTo(_motions);

            if (useScale && cg.transform is RectTransform rt)
            {
                rt.localScale = Vector3.one * inStartScale;
                LMotion.Create(rt.localScale, Vector3.one, inDuration)
                    .WithDelay(delay)
                    .WithEase(scaleInEase)
                    .Bind(x => rt.localScale = x)
                    .AddTo(_motions);
            }
        }
    }

    public void PlayOut(Action onComplete = null)
    {
        _motions.Cancel();

        if (targets.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = LSequence.Create();

        for (int i = 0; i < targets.Count; i++)
        {
            var cg = targets[i];
            if (cg == null) continue;

            cg.blocksRaycasts = false;

            var fadeHandle = LMotion.Create(cg.alpha, 0f, outDuration)
                .WithEase(outEase)
                .Bind(x => cg.alpha = x);
            seq.Insert(i * staggerDelay, fadeHandle);

            if (useScale && cg.transform is RectTransform rt)
            {
                var scaleHandle = LMotion.Create(rt.localScale, Vector3.one * outEndScale, outDuration)
                    .WithEase(scaleOutEase)
                    .Bind(x => rt.localScale = x);
                seq.Insert(i * staggerDelay, scaleHandle);
            }
        }

        seq.Run(builder =>
        {
            builder.WithOnComplete(() =>
            {
                foreach (var cg in targets)
                    if (cg != null) cg.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }).AddTo(_motions);
    }

    public void Stop()
    {
        _motions.Cancel();
    }

    void OnDisable()
    {
        instance = null;
        _motions.Cancel();
    }
}
}

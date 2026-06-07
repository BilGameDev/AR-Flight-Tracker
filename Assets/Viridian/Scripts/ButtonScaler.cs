using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Viridian.Utils
{
public class ButtonScaler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    [SerializeField] private float pressedScale = .9f;
    [SerializeField] private float duration = 0.1f;
    [SerializeField] private Ease ease = Ease.OutQuad;
    [SerializeField] private bool originalScaleOne = true;

    private Vector3 originalScale;
    private Button button;
    private MotionHandle _tween;

    void Awake()
    {
        originalScale = originalScaleOne ? Vector3.one : transform.localScale;
        button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button && !button.interactable) return;
        AnimateTo(originalScale * pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateTo(originalScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateTo(originalScale);
    }

    void OnMouseDown() => AnimateTo(originalScale * pressedScale);
    void OnMouseUp() => AnimateTo(originalScale);

    private void AnimateTo(Vector3 target)
    {
        if (_tween.IsActive()) _tween.Cancel();
        _tween = LMotion.Create(transform.localScale, target, duration)
            .WithEase(ease)
            .Bind(x => transform.localScale = x);
    }

    void OnDisable()
    {
        if (_tween.IsActive())
            _tween.Cancel();
    }
}
}

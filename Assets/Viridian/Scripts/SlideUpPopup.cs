using LitMotion;
using UnityEngine;

namespace Viridian.Utils
{
    public class SlideUpPopup : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup overlayGroup;
        [SerializeField] protected CanvasGroup panelGroup;

        protected Vector2 mainCurrentPosition;

        protected virtual void Open()
        {
            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0f;
                overlayGroup.TweenFade(0.5f, 0.2f).AddTo(this);
            }

            if (panelGroup != null)
            {
                var rect = (RectTransform)panelGroup.transform;
                mainCurrentPosition = rect.anchoredPosition;
                rect.gameObject.SetActive(true);
                rect.anchoredPosition = new Vector2(mainCurrentPosition.x, -3000);
                LMotion.Create(rect.anchoredPosition, mainCurrentPosition, 0.3f)
                    .WithEase(LitMotion.Ease.OutCubic)
                    .Bind(x => rect.anchoredPosition = x)
                    .AddTo(this);
            }
        }

        public virtual void Close()
        {
            var seq = LSequence.Create();
            if (overlayGroup != null)
                seq.Join(overlayGroup.TweenFade(0f, 0.15f));
            if (panelGroup != null)
            {
                var rect = (RectTransform)panelGroup.transform;
                var target = new Vector2(rect.anchoredPosition.x, -3000);
                seq.Join(LMotion.Create(rect.anchoredPosition, target, 0.3f)
                    .WithEase(LitMotion.Ease.InCubic)
                    .Bind(x => rect.anchoredPosition = x));
            }
            seq.Run(builder =>
            {
                builder.WithOnComplete(() =>
                {
                    if (this != null)
                        Destroy(gameObject);
                });
            }).AddTo(this);
        }

        protected virtual void OnDestroy()
        {
            // tweens linked via AddTo(this) cancel automatically on destroy
        }
    }
}

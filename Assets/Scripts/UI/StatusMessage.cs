using System.Collections;
using TMPro;
using UnityEngine;

namespace FlightTracker.UI
{
    public class StatusMessage : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private float fadeDuration = 1f;

        private Coroutine fadeRoutine;

        private void Awake()
        {
            if (messageText == null)
                messageText = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Show(string message)
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(FadeMessage(message));
        }

        private IEnumerator FadeMessage(string message)
        {
            if (messageText == null) yield break;

            messageText.text = message;
            messageText.color = new Color(messageText.color.r, messageText.color.g, messageText.color.b, 1f);

            yield return new WaitForSeconds(displayDuration);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                messageText.color = new Color(messageText.color.r, messageText.color.g, messageText.color.b, alpha);
                yield return null;
            }

            messageText.text = "";
        }
    }
}

using System.Collections;
using LitMotion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Viridian.Utils
{
    public class SceneLoader : MonoBehaviour
    {
        private static SceneLoader _instance;
        [SerializeField] private CanvasGroup _overlay;
        [SerializeField] private float _fadeDuration = 0.25f;

        private bool _isLoading;

        public static void LoadScene(string sceneName, bool showOverlay = true, System.Action onCompleted = null)
        {
            CreateInstance();
            if (_instance._isLoading) return;
            
            _instance.StartCoroutine(_instance.LoadSceneRoutine(sceneName, showOverlay, onCompleted));
        }

        private IEnumerator LoadSceneRoutine(string sceneName, bool showOverlay, System.Action onCompleted)
        {
            _isLoading = true;
            _overlay.gameObject.SetActive(true);

            // 1. Smooth Fade In
            if (showOverlay)
                yield return _overlay.TweenFade(1f, _fadeDuration).ToYieldInstruction();

            // 2. Local UI tweens will auto-cancel when their GameObjects are destroyed on scene unload

            // 3. Load the Scene Async and wait until complete
            var asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!asyncOp.isDone)
                yield return null;

            // 4. Smooth Fade Out
            if (showOverlay)
                yield return _overlay.TweenFade(0f, _fadeDuration).ToYieldInstruction();
            
            _overlay.gameObject.SetActive(false);
            _isLoading = false;
            
            onCompleted?.Invoke();
        }

        private static void CreateInstance()
        {
            if (_instance != null) return;

            var prefab = Resources.Load<SceneLoader>("Popups/SceneLoaderCanvas");
            if (prefab == null)
            {
                Debug.LogError("[SceneLoader] Popups/SceneLoaderCanvas not found in Resources.");
                return;
            }

            _instance = Instantiate(prefab);
            DontDestroyOnLoad(_instance.gameObject);
            
            // Ensure starting baseline state
            _instance._overlay.alpha = 0f;
            _instance._overlay.gameObject.SetActive(false);
        }
    }
}

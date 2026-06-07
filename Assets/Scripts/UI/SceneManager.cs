using UnityEngine;
using Viridian.Utils;

public class SceneManager : MonoBehaviour
{
    IUIEvents uiEvents;

    void OnEnable()
    {
        uiEvents = AppContext.Get<IUIEvents>();
        uiEvents.OnSceneRequested += HandleSceneRequest;
    }

    void OnDisable()
    {
        uiEvents.OnSceneRequested -= HandleSceneRequest;
    }

    void HandleSceneRequest(string sceneName)
    {
        SceneLoader.LoadScene(sceneName);
    }
}

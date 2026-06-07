using System.Collections;
using FlightTracker.Services;
using TMPro;
using UnityEngine;
using Viridian.Utils;

public class BootstrapManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;

    private IUIEvents uiEvents;

    void OnEnable()
    {
        uiEvents = AppContext.Get<IUIEvents>();
    }

    async void Start()
    {
        Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;

        SetStatus("Logging in to OpenSky...");

        try
        {
            bool ok = await AppContext.Get<IOpenSkyService>().AuthenticateAsync();

            if (ok)
            {
                SetStatus("Logged in. Loading scene...");
            }
            else
            {
                SetStatus("Login failed. Check credentials.");
            }
        }
        catch (System.Exception e)
        {
            SetStatus($"Login error: {e.Message}");
        }

        StartCoroutine(WaitForLogo());
    }

    IEnumerator WaitForLogo()
    {
        yield return new WaitForSeconds(2f);
        uiEvents.RequestScene("Home");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log($"[Bootstrap] {message}");
    }
}

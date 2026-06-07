using System;
using Viridian.Utils;

public class UIEvents : IUIEvents
{
    public event Action OnBackRequested;
    public event Action OnScanRequested;
    public event Action OnResetRequested;
    public event Action<string> OnSceneRequested;
    public event Action OnPrivacyRequested;

    public void Dispose()
    {
        OnBackRequested = null;
        OnScanRequested = null;
        OnResetRequested = null;
        OnSceneRequested = null;
        OnPrivacyRequested = null;
    }

    public void RequestScan()
    {
        OnScanRequested?.Invoke();
    }

    public void RequestBack()
    {
        OnBackRequested?.Invoke();
    }

    public void RequestPrivacy()
    {
        OnPrivacyRequested?.Invoke();
    }

    public void RequestReset()
    {
        OnResetRequested?.Invoke();
    }

    public void RequestScene(string gameScene)
    {
        OnSceneRequested?.Invoke(gameScene);
    }
}

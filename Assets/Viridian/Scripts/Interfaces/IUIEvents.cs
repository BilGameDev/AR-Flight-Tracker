using System;

namespace Viridian.Utils
{
    public interface IUIEvents : IDisposable
    {
        event Action OnBackRequested;
        event Action OnResetRequested;
        event Action<string> OnSceneRequested;
        event Action OnPrivacyRequested;

        void RequestBack();
        void RequestReset();
        void RequestScene(string gameScene);
        void RequestPrivacy();
    }
}

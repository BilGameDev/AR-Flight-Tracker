using FlightTracker.Configuration;
using FlightTracker.Services;
using FlightTracker.Utilities;
using UnityEngine;
using Viridian.Utils;

public class FlightServiceManager : ServiceManager
{
    [SerializeField] FlightTrackerConfig config;

    void Awake()
    {
        AppContext.Register<IUIEvents>(new UIEvents());
        AppContext.Register(config);

        ICredentialProvider credentials = SecretCredentialsProvider.LoadFromResources();
        IOpenSkyService openSky = new OpenSkyService(credentials);
        AppContext.Register(openSky);

        IUserLocationService locationService = new UserLocationService(this);
        AppContext.Register(locationService);

        AppContext.Register(new FlightQueryService(openSky));
        AppContext.Register(new FlightDataCache(config.RefreshIntervalSeconds));
        AppContext.Register(new AircraftMovementSimulator());
    }
}

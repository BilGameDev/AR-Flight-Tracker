using System;

namespace FlightTracker.Services
{
    public interface IUserLocationService
    {
        event Action OnLocationUpdated;
        event Action OnPermissionDenied;
        bool IsInitialized { get; }
        bool PermissionGranted { get; }
        double Latitude { get; }
        double Longitude { get; }
        double Altitude { get; }
        float HorizontalAccuracy { get; }
        void StartLocationService();
        void StopLocationService();
    }
}

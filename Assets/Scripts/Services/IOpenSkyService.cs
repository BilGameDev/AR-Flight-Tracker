using System.Collections.Generic;
using System.Threading.Tasks;
using FlightTracker.Data;

namespace FlightTracker.Services
{
    public interface IOpenSkyService
    {
        Task<bool> AuthenticateAsync();
        Task<List<AircraftState>> GetAllStatesAsync(FlightBounds? bounds = null);
        Task<List<AircraftState>> GetStatesByCallsignAsync(string callsign);
        Task<List<AircraftState>> GetStatesByIcao24Async(string icao24);
        Task<List<AircraftState>> GetStatesInAreaAsync(FlightBounds bounds);
    }
}

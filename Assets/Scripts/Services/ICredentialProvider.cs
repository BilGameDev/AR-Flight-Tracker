namespace FlightTracker.Services
{
    public interface ICredentialProvider
    {
        string ClientId { get; }
        string ClientSecret { get; }
    }
}

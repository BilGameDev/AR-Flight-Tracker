using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using FlightTracker.Data;

namespace FlightTracker.Services
{
    public class OpenSkyService : IOpenSkyService
    {
        private const string BaseUrl = "https://opensky-network.org/api";
        private const string TokenUrl = "https://auth.opensky-network.org/auth/realms/opensky-network/protocol/openid-connect/token";
        private const int MaxRetries = 3;
        private const float RetryDelaySeconds = 2f;

        private readonly ICredentialProvider credentials;
        private string accessToken;
        private double tokenExpiry;
        private bool authAttempted;
        private bool useFallback;

        public async Task<bool> AuthenticateAsync()
        {
            authAttempted = false;
            useFallback = false;
            accessToken = null;

            await EnsureAuthenticated();

            return !string.IsNullOrEmpty(accessToken) || useFallback;
        }

        public OpenSkyService(ICredentialProvider credentials = null)
        {
            this.credentials = credentials;
        }

        public async Task<List<AircraftState>> GetAllStatesAsync(FlightBounds? bounds = null)
        {
            string url = $"{BaseUrl}/states/all";
            if (bounds.HasValue)
            {
                var b = bounds.Value;
                url += $"?lamin={b.MinLatitude.ToString("F4", CultureInfo.InvariantCulture)}" +
                       $"&lomin={b.MinLongitude.ToString("F4", CultureInfo.InvariantCulture)}" +
                       $"&lamax={b.MaxLatitude.ToString("F4", CultureInfo.InvariantCulture)}" +
                       $"&lomax={b.MaxLongitude.ToString("F4", CultureInfo.InvariantCulture)}";
            }

            var json = await GetWithRetryAsync(url);
            return ParseFlightData(json);
        }

        public async Task<List<AircraftState>> GetStatesByCallsignAsync(string callsign)
        {
            var allStates = await GetAllStatesAsync();
            return allStates.FindAll(s =>
                !string.IsNullOrEmpty(s.Callsign) &&
                s.Callsign.Trim().Equals(callsign.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<AircraftState>> GetStatesByIcao24Async(string icao24)
        {
            string url = $"{BaseUrl}/states/all?icao24={Uri.EscapeDataString(icao24)}";
            var json = await GetWithRetryAsync(url);
            return ParseFlightData(json);
        }

        public async Task<List<AircraftState>> GetStatesInAreaAsync(FlightBounds bounds)
        {
            string url = $"{BaseUrl}/states/all" +
                         $"?lamin={bounds.MinLatitude.ToString("F4", CultureInfo.InvariantCulture)}" +
                         $"&lomin={bounds.MinLongitude.ToString("F4", CultureInfo.InvariantCulture)}" +
                         $"&lamax={bounds.MaxLatitude.ToString("F4", CultureInfo.InvariantCulture)}" +
                         $"&lomax={bounds.MaxLongitude.ToString("F4", CultureInfo.InvariantCulture)}";
            var json = await GetWithRetryAsync(url);
            Debug.Log($"OpenSky URL: {url}");
            if (json != null && json.Length > 0)
            {
                try { System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "opensky_response.json"), json); }
                catch { }
            }
            return ParseFlightData(json);
        }

        private async Task<string> GetWithRetryAsync(string url)
        {
            if (!useFallback)
                await EnsureAuthenticated();

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    using var request = UnityWebRequest.Get(url);
                    if (!useFallback)
                        ApplyAuthHeader(request);

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                        return request.downloadHandler.text;

                    Debug.LogWarning($"OpenSky API error (attempt {attempt + 1}): {request.error}");

                    if (request.responseCode == 401)
                    {
                        Debug.Log("Auth rejected. Falling back to unauthenticated requests.");
                        useFallback = true;
                        continue;
                    }

                    if (request.responseCode == 429)
                    {
                        int wait = (int)(RetryDelaySeconds * 2000 * (attempt + 1));
                        await Task.Delay(wait);
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"OpenSky request failed (attempt {attempt + 1}): {e.Message}");
                }

                await Task.Delay((int)(RetryDelaySeconds * 1000));
            }

            return null;
        }

        private async Task EnsureAuthenticated()
        {
            if (authAttempted) return;
            authAttempted = true;

            if (credentials == null) return;

            await FetchOAuthToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                Debug.LogWarning("OAuth2 failed. Running unauthenticated.");
                useFallback = true;
            }
        }

        private async Task FetchOAuthToken()
        {
            try
            {
                var form = new WWWForm();
                form.AddField("grant_type", "client_credentials");
                form.AddField("client_id", credentials.ClientId);
                form.AddField("client_secret", credentials.ClientSecret);

                using var request = UnityWebRequest.Post(TokenUrl, form);

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<OAuthTokenResponse>(request.downloadHandler.text);
                    if (response != null && !string.IsNullOrEmpty(response.access_token))
                    {
                        accessToken = response.access_token;
                        tokenExpiry = Time.realtimeSinceStartup + Mathf.Max(response.expires_in - 60, 60);
                        Debug.Log("OpenSky OAuth2 token acquired.");
                    }
                    else
                    {
                        Debug.LogError($"OAuth2 response missing access_token: {request.downloadHandler.text}");
                    }
                }
                else
                {
                    Debug.LogError($"OAuth2 token request failed ({request.responseCode}): {request.downloadHandler.text}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"OAuth2 token request exception: {e.Message}");
            }
        }

        private void ApplyAuthHeader(UnityWebRequest request)
        {
            if (credentials == null) return;

            if (!string.IsNullOrEmpty(accessToken))
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        }

        private static List<AircraftState> ParseFlightData(string json)
        {
            var results = new List<AircraftState>();
            if (string.IsNullOrEmpty(json)) return results;

            try
            {
                string statesJson = ExtractStatesArray(json);
                if (string.IsNullOrEmpty(statesJson))
                {
                    Debug.LogWarning("OpenSky response has no states array.");
                    return results;
                }

                var rawStates = ParseJaggedStringArray(statesJson);
                Debug.Log($"Parsed {rawStates.Count} raw state vectors.");

                foreach (var raw in rawStates)
                {
                    if (raw.Count < 17) continue;

                    var state = new AircraftState();
                    state.Icao24 = raw[0] ?? "";
                    state.Callsign = raw[1];
                    state.OriginCountry = raw[2] ?? "";
                    state.TimePosition = ParseNullableLong(raw[3]);
                    state.LastContact = ParseLong(raw[4], 0);
                    state.Longitude = ParseNullableDouble(raw[5]);
                    state.Latitude = ParseNullableDouble(raw[6]);
                    state.Altitude = ParseNullableDouble(raw[7]);
                    state.OnGround = raw.Count > 8 && ParseBool(raw[8]);
                    state.Velocity = ParseNullableDouble(raw[9]);
                    state.Heading = ParseNullableDouble(raw[10]);
                    state.VerticalRate = ParseNullableDouble(raw[11]);
                    state.GeoAltitude = raw.Count > 13 ? ParseNullableDouble(raw[13]) : null;
                    state.Squawk = raw.Count > 14 ? raw[14] : null;
                    state.Spi = raw.Count > 15 && ParseBool(raw[15]);
                    state.Source = raw.Count > 16
                        ? (AircraftState.PositionSource)ParseInt(raw[16], 4)
                        : AircraftState.PositionSource.Unknown;

                    if (state.HasPosition)
                        results.Add(state);
                }

                Debug.Log($"After filtering: {results.Count} aircraft with valid positions.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse OpenSky response: {e.Message}");
            }

            return results;
        }

        private static List<List<string>> ParseJaggedStringArray(string json)
        {
            var result = new List<List<string>>();
            int i = 0;
            int length = json.Length;

            while (i < length && json[i] <= ' ') i++;
            if (i >= length || json[i] != '[') return result;
            i++;

            while (i < length)
            {
                while (i < length && json[i] <= ' ') i++;
                if (i >= length) break;

                if (json[i] == ']') break;

                if (json[i] != '[') { i++; continue; }
                i++;

                var inner = new List<string>();
                while (i < length)
                {
                    while (i < length && json[i] <= ' ') i++;
                    if (i >= length) break;

                    if (json[i] == ']') { i++; break; }
                    if (json[i] == ',') { i++; continue; }

                    if (json[i] == '"')
                    {
                        i++;
                        int start = i;
                        while (i < length && json[i] != '"')
                        {
                            if (json[i] == '\\') i++;
                            i++;
                        }
                        inner.Add(json.Substring(start, i - start));
                        if (i < length) i++;
                    }
                    else if (json[i] == 'n' && i + 3 < length && json.Substring(i, 4) == "null")
                    {
                        inner.Add(null);
                        i += 4;
                    }
                    else if (json[i] == 't' && i + 3 < length && json.Substring(i, 4) == "true")
                    {
                        inner.Add("true");
                        i += 4;
                    }
                    else if (json[i] == 'f' && i + 4 < length && json.Substring(i, 5) == "false")
                    {
                        inner.Add("false");
                        i += 5;
                    }
                    else
                    {
                        int start = i;
                        while (i < length && json[i] != ',' && json[i] != ']' && json[i] > ' ')
                            i++;
                        inner.Add(json.Substring(start, i - start));
                    }
                }

                result.Add(inner);

                while (i < length && json[i] <= ' ') i++;
                if (i < length && json[i] == ',') i++;
            }

            return result;
        }

        private static string ExtractStatesArray(string json)
        {
            int idx = json.IndexOf("\"states\":", StringComparison.Ordinal);
            if (idx < 0) return null;

            idx += 9;
            while (idx < json.Length && json[idx] <= ' ') idx++;
            if (idx >= json.Length || json[idx] != '[') return null;

            int depth = 0;
            int start = idx;
            for (int i = start; i < json.Length; i++)
            {
                if (json[i] == '[') depth++;
                else if (json[i] == ']') { depth--; if (depth == 0) return json.Substring(start, i - start + 1); }
                else if (json[i] == '"')
                {
                    i++;
                    while (i < json.Length && json[i] != '"')
                    {
                        if (json[i] == '\\') i++;
                        i++;
                    }
                }
            }
            return null;
        }

        private static long ParseLong(string s, long defaultValue)
        {
            if (s == null) return defaultValue;
            return long.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : defaultValue;
        }

        private static int ParseInt(string s, int defaultValue)
        {
            if (s == null) return defaultValue;
            return int.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : defaultValue;
        }

        private static double? ParseNullableDouble(string s)
        {
            if (s == null) return null;
            return double.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
        }

        private static long? ParseNullableLong(string s)
        {
            if (s == null) return null;
            return long.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
        }

        private static bool ParseBool(string s)
        {
            return s == "true";
        }

        [Serializable]
        private class OAuthTokenResponse
        {
            public string access_token;
            public string token_type;
            public int expires_in;
        }
    }
}

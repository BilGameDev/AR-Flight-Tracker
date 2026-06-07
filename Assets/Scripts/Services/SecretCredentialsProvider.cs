using System.IO;
using UnityEngine;

namespace FlightTracker.Services
{
    public class SecretCredentialsProvider : ICredentialProvider
    {
        public string ClientId { get; }
        public string ClientSecret { get; }
        public string Username { get; }
        public string Password { get; }

        private SecretCredentialsProvider(string clientId, string clientSecret)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        public static ICredentialProvider LoadFromResources(string resourcePath = "Secrets/credentials")
        {
            var textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null)
            {
                Debug.LogWarning($"Credentials not found at Resources/{resourcePath}. Running without auth.");
                return null;
            }

            var json = JsonUtility.FromJson<CredentialsJson>(textAsset.text);
            if (json == null)
            {
                Debug.LogWarning("Failed to parse credentials JSON.");
                return null;
            }

            if (!string.IsNullOrEmpty(json.clientId) && !string.IsNullOrEmpty(json.clientSecret))
            {
                Debug.Log("Loaded OAuth2 client credentials.");
                return new SecretCredentialsProvider(json.clientId, json.clientSecret);
            }

            Debug.LogWarning("Credentials file present but no valid auth fields found.");
            return null;
        }

        [System.Serializable]
        private class CredentialsJson
        {
            public string clientId;
            public string clientSecret;
        }
    }
}

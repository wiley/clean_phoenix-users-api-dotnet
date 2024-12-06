using System;

using Newtonsoft.Json;

namespace WLSUser.Domain.Models.Authentication
{
    public class AuthResponseV4
    {
        private int _expiresIn = 0;
        private int _refreshExpiresIn = 0;
        private DateTime _expiresAt = DateTime.UtcNow.AddDays(-1);
        private DateTime _refreshExpiresAt = DateTime.UtcNow.AddDays(-1);

        [JsonProperty("token_type")]
        public string TokenType { get; set; }
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        [JsonProperty("expires_in")]
        public int ExpiresIn
        {
            get { return _expiresIn; }
            set { _expiresIn = value; _expiresAt = DateTime.UtcNow.AddSeconds(value); }
        }

#if !DEBUG
        [JsonIgnore]
#endif
        [JsonProperty("expires_at")]
        public DateTime ExpiresAt { get { return _expiresAt; } }
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
        [JsonProperty("refresh_expires_in")]
        public int RefreshExpiresIn
        {
            get { return _refreshExpiresIn; }
            set { _refreshExpiresIn = value; _refreshExpiresAt = DateTime.UtcNow.AddSeconds(value); }
        }

#if !DEBUG
        [JsonIgnore]
#endif
        [JsonProperty("refresh_expires_at")]
        public DateTime RefreshExpiresAt { get { return _refreshExpiresAt; } }
    }
}

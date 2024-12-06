using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;
using WLSUser.Services.Interfaces;

namespace WLSUser.Services.Authentication
{
    public class KeyCloakService : IKeyCloakService
    {
        private const string GRANT_TYPE_TOKEN_EXCHANGE = "urn:ietf:params:oauth:grant-type:token-exchange";
        private const string GRANT_TYPE_PASSWORD = "password";
        private readonly string _keycloakPasswordSalt;
        private readonly string _keycloakPublicClientId;
        private readonly string _keycloakUsersUrl;
        private readonly string _keycloakTokenUrl;
        private readonly string _keycloakclientId;
        private readonly string _keycloakSecret;
        private readonly FormUrlEncodedContent _keycloakClientCredentials;
        private readonly ILogger<KeyCloakService> _logger;
        private readonly HttpClient _client;
        private static string _keycloakAccessToken = "uninitialized";

        public KeyCloakService(ILogger<KeyCloakService> logger, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _client = clientFactory.CreateClient("keycloak");

            _keycloakUsersUrl = Environment.GetEnvironmentVariable("KEYCLOAK_USERS_URL");
            _keycloakTokenUrl = Environment.GetEnvironmentVariable("KEYCLOAK_TOKEN_URL");

            _keycloakclientId = Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_ID");
            _keycloakSecret = Environment.GetEnvironmentVariable("KEYCLOAK_SECRET");
            _keycloakPasswordSalt = Environment.GetEnvironmentVariable("KEYCLOAK_PASSWORD_SALT");
            _keycloakPublicClientId = Environment.GetEnvironmentVariable("KEYCLOAK_PUBLIC_CLIENT_ID");

            _keycloakClientCredentials = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _keycloakclientId },
                { "client_secret", _keycloakSecret }
            });
        }

        public async Task<AuthResponseV4> GetPasswordTokens(UserModel user)
        {
            var jsonUser = new JArray();
            try
            {
                var encodedUsername = Uri.EscapeDataString(user.Username);
                var response = await SendKeycloakAsync(HttpMethod.Get, $"{_keycloakUsersUrl}/?username={encodedUsername}&exact=true");
                if (response.StatusCode == HttpStatusCode.OK)
                    jsonUser = JArray.Parse(await response.Content.ReadAsStringAsync());

                string passwordHashed = GetPasswordHash(user.Username);
                await CheckUserKeyCloak(jsonUser, user, passwordHashed);

                var tResponse = await GetTokensPublicClient(user, passwordHashed);

                var result = JsonConvert.DeserializeObject<AuthResponseV4>(await tResponse.Content.ReadAsStringAsync());
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting password token, please check the error message: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public async Task<AuthResponseV4> GetApiTokens() 
        {
            var contentRaw = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _keycloakclientId },
                { "client_secret", _keycloakSecret }
            };
            FormUrlEncodedContent content = new FormUrlEncodedContent(contentRaw);
            var response = await SendKeycloakAsync(HttpMethod.Post, $"{_keycloakTokenUrl}", content);
            var tokens = JsonConvert.DeserializeObject<AuthResponseV4>(await response.Content.ReadAsStringAsync());

            return tokens;
        }
        private string GetPasswordHash(string username)
        {
            //this is the exact method used by CK2SGP, keep the consistency
            using (SHA256 mySHA256 = SHA256.Create())
            {
                Byte[] inputBytes = Encoding.UTF8.GetBytes(username + _keycloakPasswordSalt);
                Byte[] hashedBytes = mySHA256.ComputeHash(inputBytes);

                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        public async Task<Dictionary<string, string>> GetJwt(string username)
        {
            try
            {
                var contentRaw = new Dictionary<string, string>
                {
                    { "grant_type", GRANT_TYPE_TOKEN_EXCHANGE },
                    { "client_id", _keycloakclientId },
                    { "client_secret", _keycloakSecret },
                    { "requested_subject", username }
                };

                FormUrlEncodedContent content = new FormUrlEncodedContent(contentRaw);

                var response = await SendKeycloakAsync(HttpMethod.Post, _keycloakTokenUrl, content);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new UnauthorizedAccessException($"Failed to retrieve Keycloak Tokens: {response}");
                }

                var tokens = JsonConvert.DeserializeObject<AuthResponseV4>(await response.Content.ReadAsStringAsync());
                return new Dictionary<string, string>
                {
                    { "access_token", tokens.AccessToken },
                    { "refresh_token", tokens.RefreshToken },
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting JWT, please check the error message: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> UserExists(string username)
        {
            return (await GetKeycloakUsers(username)).Any();
        }

        public async Task<bool> CreateUser(string username)
        {
            string passwordHashed;

            using (SHA256 mySHA256 = SHA256.Create())
            {
                Byte[] inputBytes = Encoding.UTF8.GetBytes(username + _keycloakPasswordSalt);
                Byte[] hashedBytes = mySHA256.ComputeHash(inputBytes);

                passwordHashed = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }

            var contentRaw = new Dictionary<string, string>
            {
                { "username", username },
                { "password", passwordHashed },
                { "grant_type", "password" }
            };

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakUsersUrl}");
            var content = new StringContent(JsonConvert.SerializeObject(contentRaw), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await SendKeycloakAsync(HttpMethod.Post, _keycloakUsersUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(string.Format("Failed to create user: {0}", response.ToString()));

                return false;
            }

            return true;
        }

        public async Task DeleteUser(string keycloakUsername)
        {
            var keycloakUsers = await GetKeycloakUsers(keycloakUsername);

            if(keycloakUsers.Count == 0) {
                _logger.LogDebug("The user {KeycloakUsername} do not exist in Keycloack.", keycloakUsername);
                return;
            }

            var keycloakUserId = keycloakUsers.First["id"].ToString();

            _logger.LogDebug("Removing user {KeycloakUsername} from keycloack", keycloakUsername);
            try
            {
                var uResponse = await SendKeycloakAsync(HttpMethod.Delete, $"{_keycloakUsersUrl}/{keycloakUserId}");
                // user not found is not an error as a user can be created without having a Keycloak account
                if (!uResponse.IsSuccessStatusCode)
                {
                    throw new UnauthorizedAccessException($"Failed to delete Keycloak User: {uResponse}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Unable to delete the user with the username {KeycloakUsername}, please check the error message: {ErrorMessage}",
                    keycloakUsername,
                    ex.Message
                );
                throw new Exception(ex.Message);
            }
        }

        public async Task Logout(string keycloakUserId)
        {
            try
            {
                string url = $"{_keycloakUsersUrl}/{keycloakUserId}/logout";

                HttpResponseMessage response = await SendKeycloakAsync(HttpMethod.Post, url);

                if (response.IsSuccessStatusCode)
                    return;

                throw new UnauthorizedAccessException($"Failed to logout: {response}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error logging out, please check the error message: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        private async Task<HttpResponseMessage> SendKeycloakAsync(HttpMethod method, string keycloakUri)
        {
            return await SendKeycloakAsync(method, keycloakUri, null);
        }

        private async Task<HttpResponseMessage> SendKeycloakAsync(HttpMethod method, string keycloakUri, HttpContent content)
        {
            HttpResponseMessage response = null;
            bool refreshed = false;
            while (true)
            {
                HttpRequestMessage message = new HttpRequestMessage(method, keycloakUri);
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _keycloakAccessToken);
                message.Content = content;
                response = await _client.SendAsync(message);

                if (refreshed || response.StatusCode != HttpStatusCode.Unauthorized)
                    return response;
                else if (!(refreshed = RefreshAccessToken()))
                    break;
            }

            return response;
        }

        private bool RefreshAccessToken()
        {
            lock (_keycloakAccessToken)
            {
                HttpResponseMessage acResponse = _client.PostAsync(_keycloakTokenUrl, _keycloakClientCredentials).Result;
                if (acResponse.StatusCode == HttpStatusCode.OK)
                {
                    JObject result = JObject.Parse(acResponse.Content.ReadAsStringAsync().Result);
                    _keycloakAccessToken = result["access_token"].ToString();
                    return true;
                }
            }
            return false;
        }
        private async Task CheckUserKeyCloak(JArray jsonUser, UserModel user, string passwordHashed)
        {
            try
            {
                //checks if the user exists in keycloak
                if (jsonUser.Count != 0)
                {
                    //user exists, that's nice but we need to make sure it's coherent
                    var attributes = jsonUser[0]["attributes"].ToObject<JObject>();
                    bool found = false;
                    foreach (var i in attributes.Properties())
                    {
                        //Here we need to check if there is coherency with UserID and user_id
                        if (i.Name == "user_id"
                            && Int64.Parse(i.Value[0].ToString()) == user.UserID)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        throw new UnauthorizedAccessException($"UserID not found in Keycloak: {user.UserID}");
                }
                else
                {
                    //user does not exist and needs to be creatad
                    //we need to set the user_id attribute == UserID
                    JObject userPayload = new JObject {
                    {"username", user.Username},
                    {"enabled", true},
                    {"credentials", ""},
                    {"attributes", ""},
                    };
                    userPayload["credentials"] = new JArray {
                        {
                            new JObject {
                                {"temporary", false},
                                {"value", passwordHashed},
                                {"type", "password"},
                            }
                        }
                    };
                    userPayload["attributes"] = new JObject {
                        {"user_id" , user.UserID},
                        {"email" , user.Email}
                    };

                    var ucontent = new StringContent(userPayload.ToString(), Encoding.UTF8, "application/json");
                    var uResponse = await SendKeycloakAsync(HttpMethod.Post, _keycloakUsersUrl, ucontent);
                    if (!uResponse.IsSuccessStatusCode)
                    {
                        throw new UnauthorizedAccessException($"Failed to create Keycloak User: {uResponse}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user on keyCloak, please check the error message: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        private async Task<JArray> GetKeycloakUsers(string username)
        {
            string urlQuery = $"{_keycloakUsersUrl}/?username={username}";

            HttpResponseMessage response = await SendKeycloakAsync(HttpMethod.Get, urlQuery);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Failed to check user existence: {Response}", response.ToString());
                return new JArray();
            }

            return JArray.Parse(await response.Content.ReadAsStringAsync());
        }

        private async Task<HttpResponseMessage> GetTokensPublicClient(UserModel user, string passwordHashed)
        {
            //now we ask the tokens using the public client and not the private one
            //this is important because only a public client generated token can be refreshed by the browser
            var contentRaw = new Dictionary<string, string>
            {
                { "grant_type",  GRANT_TYPE_PASSWORD },
                { "client_id", _keycloakPublicClientId },
                { "username", user.Username },
                { "password" , passwordHashed }
            };

            FormUrlEncodedContent content = new FormUrlEncodedContent(contentRaw);

            var response = await SendKeycloakAsync(HttpMethod.Post, _keycloakTokenUrl, content);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new UnauthorizedAccessException($"Failed to retrieve Keycloak Tokens: {response}");
            }

            return response;
        }
    }
}

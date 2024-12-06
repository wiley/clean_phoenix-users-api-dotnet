using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WLSUser.Domain.Exceptions;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Interfaces;
using WLSUser.Services.Interfaces;

namespace WLSUser.Services
{
    public class SsoService : ISsoService
    {
        private readonly HttpClient _client;
        private readonly JwtSecurityTokenHandler _handler;
        private readonly ILogger<SsoService> _logger;
        private readonly IAppConfig _appConfig;

        public SsoService(IHttpClientFactory clientFactory, JwtSecurityTokenHandler handler, ILogger<SsoService> logger, IAppConfig appConfig)
        {
            _client = clientFactory.CreateClient("sso");
            _handler = handler;
            _logger = logger;
            _appConfig = appConfig;
        }

        public FormUrlEncodedContent CreateSsoContent(Federation federation, string code, Dictionary<string, string> clientSecretDictionary)
        {
            var data = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", federation.RedirectUrl }
            };

            if (clientSecretDictionary != null)
            {
                clientSecretDictionary.ToList().ForEach(x => data.Add(x.Key, x.Value));
            }

            return new FormUrlEncodedContent(data);
        }

        public Dictionary<string, string> GetClientSecretDictionary(Federation federation)
        {
            var data = new Dictionary<string, string>
            {
                { "client_id", federation.OpenIdClientId },
                { "client_secret", GetDecryptedSecret(federation.OpenIdClientSecret) }
            };

            return data;
        }

        public string GetClientSecretBasicAuthentication(Federation federation)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(federation.OpenIdClientId + ":" + GetDecryptedSecret(federation.OpenIdClientSecret));
            return System.Convert.ToBase64String(bytes);
        }

        public async Task<OpenIdToken> GetOpenIdToken(Federation federation, FormUrlEncodedContent content, string clientSecretBasicAuthentication)
        {
            try
            {
                if (!string.IsNullOrEmpty(clientSecretBasicAuthentication))
                {
                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", clientSecretBasicAuthentication);
                }
                var response = await _client.PostAsync(federation.OpenIdTokenUrl, content);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogWarning("GetOpenIdToken - OpenIdUrl Failed - {Name}, {StatusCode}", federation.Name, response.StatusCode);
                    throw new OpenIdException("Failed to request Open ID Token");
                }

                var result = JObject.Parse(await response.Content.ReadAsStringAsync());
                if (result == null || result["id_token"] == null)
                {
                    _logger.LogWarning("GetOpenIdToken - id_token not found - {Name}", federation.Name);
                    throw new OpenIdException("Failed to get Open ID Token");
                }

                var jsonToken = _handler.ReadToken(result["id_token"].ToString()) as JwtSecurityToken;
                if (jsonToken == null)
                {
                    _logger.LogWarning("GetOpenIdToken - failure parsing id_token - {Name}", federation.Name);
                    throw new OpenIdException("Failed to parse id_token");
                }
                if (jsonToken.Payload == null)
                {
                    _logger.LogWarning("GetOpenIdToken - id_token Payload empty - {Name}", federation.Name);
                    throw new OpenIdException("Failed to parse id_token");
                }
                OpenIdToken model = new OpenIdToken();
                if (jsonToken.Payload.ContainsKey("email") && jsonToken.Payload["email"] != null && !string.IsNullOrEmpty(jsonToken.Payload["email"].ToString()))
                {
                    model.Email = jsonToken.Payload["email"].ToString();
                }
                else if (jsonToken.Payload.ContainsKey("preferred_username") && jsonToken.Payload["preferred_username"] != null && !string.IsNullOrEmpty(jsonToken.Payload["preferred_username"].ToString()))
                {
                    model.Email = jsonToken.Payload["preferred_username"].ToString();
                }
                else if (jsonToken.Payload.ContainsKey("name") && jsonToken.Payload["name"] != null && !string.IsNullOrEmpty(jsonToken.Payload["name"].ToString()))
                {
                    model.Email = jsonToken.Payload["name"].ToString();
                }
                else
                {
                    string payloadKeys = "";
                    if (jsonToken.Payload.Keys != null)
                    {
                        payloadKeys = string.Join(",", jsonToken.Payload.Keys);
                    }
                    _logger.LogWarning("GetOpenIdToken - id_token payload does not contain email - {Name}, {payloadKeys}", federation.Name, payloadKeys);
                    throw new OpenIdException("Payload does not contain email");
                }

                string subject = string.Empty;
                if (jsonToken.Payload.ContainsKey("sub") && !string.IsNullOrWhiteSpace(jsonToken.Payload["sub"].ToString()))
                    subject = jsonToken.Payload["sub"].ToString();

                _logger.LogWarning($"GetOpenIdToken - Success - Federation - {federation.Name}, Email - {model.Email}, Subject - {subject}");
                return model;
            }
            catch (OpenIdException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "GetOpenIdToken - Failed - {Name}", federation.Name);
                throw new OpenIdException("Error in GetOpenIdToken");
            }
        }

        private string GetDecryptedSecret(string encryptedSecret)
        {
            try
            {
                if (_appConfig != null && !string.IsNullOrEmpty(_appConfig.PrivateKey))
                {
                    byte[] cipherText = Convert.FromBase64String(encryptedSecret);

                    using (var rsa = RSA.Create())
                    {
                        rsa.ImportFromPem(_appConfig.PrivateKey);
                        byte[] bytes = rsa.Decrypt(cipherText, RSAEncryptionPadding.Pkcs1);
                        String result = Encoding.UTF8.GetString(bytes);
                        if (!string.IsNullOrEmpty(result))
                        {
                            //Strip whitespace and CR/LF from text - possible artifacts of command line encryption of original
                            //The Client Secret is supposed to be a parameterized string
                            result = result.Replace("\r", "").Replace("\n", "").Trim();

                            return result;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation(ex, "Unable to decrypt client secret");
            }
            return encryptedSecret;
        }
    }
}

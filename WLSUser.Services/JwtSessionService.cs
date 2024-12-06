using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WLSUser.Domain.Exceptions;
using WLSUser.Domain.Models;
using WLSUser.Services.Interfaces;

namespace WLSUser.Services
{
    public class JwtSessionService : IJwtSessionService
    {
        private readonly int ExpirationTime = 36000;
        private readonly string SuffixAuthUrl = ":authUrl";
        private readonly string SuffixFederationName = ":federationName";
        private readonly string SuffixredirectUrl = ":redirectUrl";
        private readonly IRedisService _redis;
        private readonly ISsoService _ssoService;
        private readonly IKeyCloakService _keyCloakService;
        private readonly IFederationService _federationService;
        private readonly ILogger<JwtSessionService> _logger;

        public JwtSessionService(
            IRedisService redisService,
            ISsoService ssoService,
            IKeyCloakService keyCloakService,
            IFederationService federationService,
            ILogger<JwtSessionService> logger)
        {
            _redis = redisService;
            _ssoService = ssoService;
            _keyCloakService = keyCloakService;
            _federationService = federationService;
            _logger = logger;
        }

        public string GetAuthUrl(string state)
        {
            return _redis.GetString(state + SuffixAuthUrl);
        }

        public void SetAuthUrl(string state, string value)
        {
            _redis.SetString(ExpirationTime, state + SuffixAuthUrl, value);
        }

        public string GetFederationName(string state)
        {
            return _redis.GetString(state + SuffixFederationName);
        }

        public void SetFederationName(string state, string value)
        {
            _redis.SetString(ExpirationTime, state + SuffixFederationName, value);
        }

        public string GetRedirectUrl(string state)
        {
            return _redis.GetString(state + SuffixredirectUrl);
        }

        public void SetRedirectUrl(string state, string value)
        {
            _redis.SetString(ExpirationTime, state + SuffixredirectUrl, value);
        }

        public async Task<Dictionary<string, string>> CreateJwtToken(string code, string state)
        {
            string federationName = GetFederationName(state);
            Federation federation = await _federationService.GetFederationByName(federationName, (int) SiteTypeEnum.Catalyst);
            if (federation == null)
                throw new NotFoundException(String.Format("No federation with name {0}", federationName));

            Dictionary<string, string> formSecrets = new Dictionary<string, string>(2);
            string basicToken = "";

            if (federation.AuthMethod == FederationConstants.DefaultAuthMethod || string.IsNullOrEmpty(federation.AuthMethod))
            {
                formSecrets = _ssoService.GetClientSecretDictionary(federation);
            }
            else if (federation.AuthMethod == FederationConstants.AuthMethodClientSecretBasic)
            {
                basicToken = _ssoService.GetClientSecretBasicAuthentication(federation);
            }
            else
            {
                throw new NotImplementedException();
            }

            FormUrlEncodedContent content = _ssoService.CreateSsoContent(federation, code, formSecrets);
            OpenIdToken openIdToken = await _ssoService.GetOpenIdToken(federation, content, basicToken);
            if (openIdToken == null || String.IsNullOrWhiteSpace(openIdToken.Email))
                throw new NotFoundException("Email not found in openIdToken response");

            if (await _keyCloakService.UserExists(openIdToken.Email) == false)
            {
                bool created = await _keyCloakService.CreateUser(openIdToken.Email);
                if (created == false)
                    return null;
            }

            return await _keyCloakService.GetJwt(openIdToken.Email);
        }
    }
}

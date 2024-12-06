using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WLSUser.Domain.Exceptions;
using WLSUser.Domain.Models;
using WLSUser.Services;
using WLSUser.Services.Interfaces;
using Xunit;

namespace WLSUser.Tests.Services
{
    public class JwtSessionServiceTests
    {
        private JwtSessionService _jwtSessionService;
        private readonly IRedisService _redisService;
        private readonly ISsoService _ssoService;
        private readonly IKeyCloakService _keyCloakService;
        private readonly IFederationService _federationService;
        private readonly ILogger<JwtSessionService> _logger;

        public JwtSessionServiceTests()
        {
            _redisService = Substitute.For<IRedisService>();
            _ssoService = Substitute.For<ISsoService>();
            _keyCloakService = Substitute.For<IKeyCloakService>();
            _federationService = Substitute.For<IFederationService>();
            _logger = Substitute.For<ILogger<JwtSessionService>>();

            _jwtSessionService = new JwtSessionService(
                _redisService,
                _ssoService,
                _keyCloakService,
                _federationService,
                _logger);
        }

        [Fact]
        public async Task CreateJwtToken_ReturnsJwt()
        {
            Federation federation = new Federation() { Name = "ck" };
            FormUrlEncodedContent ssoContent = new FormUrlEncodedContent(new Dictionary<string, string>());

            _jwtSessionService.GetFederationName(Arg.Any<string>()).ReturnsForAnyArgs(federation.Name);
            _federationService.GetFederationByName(Arg.Any<string>(), Arg.Any<int>()).ReturnsForAnyArgs(federation);
            _ssoService.GetClientSecretDictionary(Arg.Any<Federation>()).ReturnsForAnyArgs(new Dictionary<string, string>());
            _ssoService.GetClientSecretBasicAuthentication(Arg.Any<Federation>()).ReturnsForAnyArgs("");
            _ssoService.CreateSsoContent(Arg.Any<Federation>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>()).ReturnsForAnyArgs(ssoContent);
            _ssoService.GetOpenIdToken(Arg.Any<Federation>(), Arg.Any<FormUrlEncodedContent>(), Arg.Any<string>()).ReturnsForAnyArgs(new OpenIdToken() { Email = "test@email.com" });
            _keyCloakService.UserExists(Arg.Any<string>()).ReturnsForAnyArgs(false);
            _keyCloakService.CreateUser(Arg.Any<string>()).ReturnsForAnyArgs(true);
            _keyCloakService.GetJwt(Arg.Any<string>()).ReturnsForAnyArgs(new Dictionary<string, string>());

            var response = await _jwtSessionService.CreateJwtToken("code", "state");

            Assert.IsType<Dictionary<string, string>>(response);

            await _federationService.Received(1).GetFederationByName(Arg.Any<string>(), Arg.Any<int>());
            _ssoService.Received(1).GetClientSecretDictionary(Arg.Any<Federation>());
            _ssoService.DidNotReceive().GetClientSecretBasicAuthentication(Arg.Any<Federation>());
            _ssoService.Received(1).CreateSsoContent(Arg.Any<Federation>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>());
            await _ssoService.Received(1).GetOpenIdToken(Arg.Any<Federation>(), Arg.Any<FormUrlEncodedContent>(), Arg.Any<string>());
        }

        [Fact]
        public async Task CreateJwtToken_WithNullFederation_ThrowsNotFoundException()
        {
            Federation federation = new Federation() { Name = "ck" };
            FormUrlEncodedContent ssoContent = new FormUrlEncodedContent(new Dictionary<string, string>());

            _jwtSessionService.GetFederationName(Arg.Any<string>()).ReturnsForAnyArgs(federation.Name);
            _federationService.GetFederationByName(Arg.Any<string>(), Arg.Any<int>()).ReturnsNull();

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _jwtSessionService.CreateJwtToken("code", "state");
            });

            await _federationService.Received(1).GetFederationByName(Arg.Any<string>(), Arg.Any<int>());
            _ssoService.DidNotReceive().GetClientSecretDictionary(Arg.Any<Federation>());
            _ssoService.DidNotReceive().GetClientSecretBasicAuthentication(Arg.Any<Federation>());
            _ssoService.DidNotReceive().CreateSsoContent(Arg.Any<Federation>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>());
            await _ssoService.DidNotReceive().GetOpenIdToken(Arg.Any<Federation>(), Arg.Any<FormUrlEncodedContent>(), Arg.Any<string>());
        }

        [Fact]
        public async Task CreateJwtToken_WithNullContent_ThrowsNotFoundException()
        {
            Federation federation = new Federation() { Name = "ck" };
            FormUrlEncodedContent ssoContent = new FormUrlEncodedContent(new Dictionary<string, string>());

            _jwtSessionService.GetFederationName(Arg.Any<string>()).ReturnsForAnyArgs(federation.Name);
            _federationService.GetFederationByName(Arg.Any<string>(), Arg.Any<int>()).ReturnsForAnyArgs(federation);
            _ssoService.GetClientSecretDictionary(Arg.Any<Federation>()).ReturnsForAnyArgs(new Dictionary<string, string>());
            _ssoService.GetClientSecretBasicAuthentication(Arg.Any<Federation>()).ReturnsForAnyArgs("");
            _ssoService.CreateSsoContent(Arg.Any<Federation>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>()).ReturnsNull();

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _jwtSessionService.CreateJwtToken("code", "state");
            });
            await _federationService.Received(1).GetFederationByName(Arg.Any<string>(), Arg.Any<int>());
            _ssoService.Received(1).GetClientSecretDictionary(Arg.Any<Federation>());
            _ssoService.DidNotReceive().GetClientSecretBasicAuthentication(Arg.Any<Federation>());
            _ssoService.Received(1).CreateSsoContent(Arg.Any<Federation>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>());
            await _ssoService.Received(1).GetOpenIdToken(Arg.Any<Federation>(), Arg.Any<FormUrlEncodedContent>(), Arg.Any<string>());
        }

        [Fact]
        public async Task CreateJwtToken_WithNullOpenIdToken_ThrowsNotFoundException()
        {
            Federation federation = new Federation() { Name = "ck" };
            FormUrlEncodedContent ssoContent = new FormUrlEncodedContent(new Dictionary<string, string>());

            _jwtSessionService.GetFederationName(Arg.Any<string>()).ReturnsForAnyArgs(federation.Name);
            _federationService.GetFederationByName(Arg.Any<string>(), Arg.Any<int>()).ReturnsForAnyArgs(federation);
            _ssoService.GetClientSecretDictionary(Arg.Any<Federation>()).ReturnsForAnyArgs(new Dictionary<string, string>());
            _ssoService.GetClientSecretBasicAuthentication(Arg.Any<Federation>()).ReturnsForAnyArgs("");
            _ssoService.CreateSsoContent(Arg.Any<Federation>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>()).ReturnsForAnyArgs(ssoContent);
            _ssoService.GetOpenIdToken(Arg.Any<Federation>(), Arg.Any<FormUrlEncodedContent>(), Arg.Any<string>()).ReturnsNull();

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _jwtSessionService.CreateJwtToken("code", "state");
            });
            await _federationService.Received(1).GetFederationByName(Arg.Any<string>(), Arg.Any<int>());
            _ssoService.Received(1).GetClientSecretDictionary(Arg.Any<Federation>());
            _ssoService.DidNotReceive().GetClientSecretBasicAuthentication(Arg.Any<Federation>());
            _ssoService.Received(1).CreateSsoContent(Arg.Any<Federation>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>());
            await _ssoService.Received(1).GetOpenIdToken(Arg.Any<Federation>(), Arg.Any<FormUrlEncodedContent>(), Arg.Any<string>());
        }

        [Fact]
        public async Task CreateJwtToken_WithNullEmail_ThrowsNotFoundException()
        {
            Federation federation = new Federation() { Name = "ck" };
            FormUrlEncodedContent ssoContent = new FormUrlEncodedContent(new Dictionary<string, string>());

            _jwtSessionService.GetFederationName(Arg.Any<string>()).ReturnsForAnyArgs(federation.Name);
            _federationService.GetFederationByName(Arg.Any<string>(), Arg.Any<int>()).ReturnsForAnyArgs(federation);
            _ssoService.GetClientSecretDictionary(Arg.Any<Federation>()).ReturnsForAnyArgs(new Dictionary<string, string>());
            _ssoService.GetClientSecretBasicAuthentication(Arg.Any<Federation>()).ReturnsForAnyArgs("");
            _ssoService.CreateSsoContent(Arg.Any<Federation>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>()).ReturnsForAnyArgs(ssoContent);
            _ssoService.GetOpenIdToken(Arg.Any<Federation>(), Arg.Any<FormUrlEncodedContent>(), Arg.Any<string>()).ReturnsForAnyArgs(new OpenIdToken() { Email = null });

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _jwtSessionService.CreateJwtToken("code", "state");
            });

            await _federationService.Received(1).GetFederationByName(Arg.Any<string>(), Arg.Any<int>());
            _ssoService.Received(1).GetClientSecretDictionary(Arg.Any<Federation>());
            _ssoService.DidNotReceive().GetClientSecretBasicAuthentication(Arg.Any<Federation>());
            _ssoService.Received(1).CreateSsoContent(Arg.Any<Federation>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>());
            await _ssoService.Received(1).GetOpenIdToken(Arg.Any<Federation>(), Arg.Any<FormUrlEncodedContent>(), Arg.Any<string>());
        }
    }
}

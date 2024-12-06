using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using WLS.Log.LoggerTransactionPattern;
using WLSUser.Controllers;
using WLSUser.Domain.Constants;
using WLSUser.Domain.Exceptions;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;
using WLSUser.Services.Authentication;
using WLSUser.Services.Interfaces;
using WLSUser.Tests.Util;
using Xunit;

namespace WLSUser.Tests.Controllers
{
    public class AuthControllerTests
	{
		private const string FAKEJWTTOKEN = "JWTToken";
		private readonly AuthController _authController;
		private readonly IAuthService _authService;
		private readonly ICookiesService _cookiesService;
		private readonly MockLogger<AuthController> _controllerlogger;
		private readonly MockLogger<AuthService> _servicelogger;
		private readonly IFederationService _federationService;
		private readonly IJwtSessionService _jwtSessionService;
		private readonly IUserService _userService;
		private readonly ILoggerStateFactory _loggerStateFactory;

		public AuthControllerTests()
		{
			_authController = null;
			_controllerlogger = Substitute.For<MockLogger<AuthController>>();
			_servicelogger = Substitute.For<MockLogger<AuthService>>();

			_authService = Substitute.For<IAuthService>();
			_cookiesService = Substitute.For<ICookiesService>();
			_federationService = Substitute.For<IFederationService>();
			_jwtSessionService = Substitute.For<IJwtSessionService>();
			_userService = Substitute.For<IUserService>();
			_loggerStateFactory = Substitute.For<ILoggerStateFactory>();
			_loggerStateFactory.Create(Arg.Any<string>()).ReturnsForAnyArgs(new LoggerState());


			_authController = new AuthController(
				_authService,
				_cookiesService,
				_controllerlogger,
				_federationService,
				_jwtSessionService,
				_userService,
				_loggerStateFactory);

			_authController.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext()
			};
		}

		#region Auth Tests
		[Fact]
		public async Task Auth_ReturnsBadRequestFromModelState()
		{
			AuthRequest request = new AuthRequest { Username = "SomeUsername", Password = "Doesnotmatter" };
			_authController.ModelState.AddModelError("Something", "Some Error");
			object response = await _authController.Auth(request);

			//Assert
			await _authService.DidNotReceive().Login(Arg.Any<AuthRequest>());
			Assert.IsType<BadRequestObjectResult>(response);
		}

		[Fact]
		public async Task Auth_WithRefresh_ReturnsSuccess()
		{
			//Arrange
			AuthRequest request = new AuthRequest { Username = "asdfasdfasfd", Password = "asdfas13431@#", PersistToken = true };

			var expected = TestData.AuthResponse1;
			var accessTokenFingerprint = "accessTokenFingerprint";

			_authService.Login(Arg.Any<AuthRequest>()).Returns((expected, accessTokenFingerprint));

			var response = await _authController.Auth(request);

			//Assert
			await _authService.Received(1).Login(request);

			_cookiesService.Received(1).SetCookie(Arg.Any<HttpResponse>(), "AccessTokenFingerprint", accessTokenFingerprint);

			Assert.IsType<OkObjectResult>(response);
			Assert.IsType<(AuthResponse, string)>(((OkObjectResult)response).Value);

			var value = ((OkObjectResult)response).Value;
			value.Should().BeEquivalentTo((expected, accessTokenFingerprint));

			_controllerlogger.DidNotReceive().Log(LogLevel.Warning, Arg.Is<string>(s => s.Contains("Auth")));
		}

		[Fact]
		public async Task Auth_WithoutRefresh_ReturnsSuccess()
		{
			//Arrange
			AuthRequest request = new AuthRequest { Username = "asdfasdfasfd", Password = "asdfas13431@#", PersistToken = false };

			var expected = TestData.AuthResponse1;
			var accessTokenFingerprint = "accessTokenFingerprint";

			_authService.Login(Arg.Any<AuthRequest>()).Returns((expected, accessTokenFingerprint));

			var response = await _authController.Auth(request);

			//Assert
			await _authService.Received(1).Login(request);

			_cookiesService.Received(1).SetCookie(Arg.Any<HttpResponse>(), "AccessTokenFingerprint", accessTokenFingerprint);

			Assert.IsType<OkObjectResult>(response);
			Assert.IsType<(AuthResponse, string)>(((OkObjectResult)response).Value);

			var value = ((OkObjectResult)response).Value;
			value.Should().BeEquivalentTo((expected, accessTokenFingerprint));

			_controllerlogger.DidNotReceive().Log(LogLevel.Warning, Arg.Is<string>(s => s.Contains("Auth")));
		}

		[Fact]
		public async Task Auth_BadRequestFromNull()
		{
			//Arrange
			AuthRequest request = new AuthRequest { Username = "asdfasdfasfd", Password = "asdfas13431@#" };

			var expected = TestData.AuthResponse1;
			_authService.Login(Arg.Any<AuthRequest>()).Returns((expected, null));

			var response = await _authController.Auth(null);

			//Assert
			await _authService.DidNotReceive().Login(request);
			Assert.IsType<BadRequestResult>(response);
		}

		[Fact]
		public async Task Auth_ReturnsExceptionFromCatch()
		{
			_authService.Login(Arg.Any<AuthRequest>())
				.Throws(new Exception());

			var response = await _authController.Auth(new AuthRequest { Username = "atest@test.com", Password = "test" });

			await _authService.Received(1).Login(Arg.Any<AuthRequest>());
			Assert.IsType<StatusCodeResult>(response);
			Assert.Equal((int)HttpStatusCode.InternalServerError, ((StatusCodeResult)response).StatusCode);
		}

		[Fact]
		public async Task Auth_InvalidCredentials()
		{
			AuthRequest request = new AuthRequest { Username = "asdfasdfasfd", Password = "asdfas13431@#" };

			var expected = new AuthResponse { };

			_authService.Login(Arg.Any<AuthRequest>())
				.Throws(new AuthenticationFailedException());

			var response = await _authController.Auth(request);

			//Assert
			Assert.IsType<UnauthorizedResult>(response);
			_controllerlogger.Received(1).Log(LogLevel.Warning, Arg.Is<string>(s => s.Contains($"Auth - Authentication Failed - {request.Username}")));
		}

		#endregion

		#region ExchangeToken Tests
		[Fact]
		public async Task ExchangeToken_ReturnsSuccess()
		{
			string token = "6273CEA6D3C809E39D0A8C66A58D4EA2";
			var expected = TestData.AuthResponse1;

			_authService.ExchangeToken(Arg.Any<string>()).Returns((expected, "tokenFingerprint"));
			var response = await _authController.ExchangeToken(token);
			await _authService.Received(1).ExchangeToken(token);

			Assert.IsType<OkObjectResult>(response);
			Assert.IsType<(AuthResponse, string)>(((OkObjectResult)response).Value);
			var value = ((AuthResponse, string))((OkObjectResult)response).Value;
			value.Should().BeEquivalentTo((expected, "tokenFingerprint"));

			_controllerlogger.Received(1).Log(LogLevel.Information, Arg.Is<string>(s => s.Contains($"ExchangeToken - {token}")));
			_controllerlogger.Received(1).Log(LogLevel.Information, Arg.Is<string>(s => s.Contains($"ExchangeToken - Authentication Success - {token} > {expected.AccessToken}")));
		}

		[Fact]
		public async Task ExchangeToken_ReturnsBadRequest()
		{
			string token = " 	 ";
			var response = await _authController.ExchangeToken(token);
			await _authService.DidNotReceive().ExchangeToken(Arg.Any<string>());

			Assert.IsType<BadRequestObjectResult>(response);
			Assert.IsType<string>(((BadRequestObjectResult)response).Value);
			var value = ((BadRequestObjectResult)response).Value;
			Assert.Equal("Empty Token", value);
			_controllerlogger.Received(1).Log(LogLevel.Warning, Arg.Is<string>(s => s.Contains("ExchangeToken - Empty Token Received")));
		}

		[Fact]
		public async Task ExchangeToken_ReturnsNotFound()
		{
			string token = "6273CEA6D3C809E39D0A8C66A58D4EA2";
			_authService.ExchangeToken(Arg.Any<string>()).Returns((null, "tokenFingerprint"));

			var response = await _authController.ExchangeToken(token);
			await _authService.Received(1).ExchangeToken(Arg.Any<string>());

			Assert.IsType<NotFoundResult>(response);
		}

		[Fact]
		public async void ExchangeToken_ReturnsUnauthorized()
		{
			string token = "6273CEA6D3C809E39D0A8C66A58D4EA2";

			_authService.ExchangeToken(Arg.Any<string>()).Throws(new AuthenticationFailedException());
			var response = await _authController.ExchangeToken(token);
			await _authService.Received(1).ExchangeToken(Arg.Any<string>());

			Assert.IsType<UnauthorizedResult>(response);
			_controllerlogger.Received(1).Log(LogLevel.Information, Arg.Is<string>(s => s.Contains($"ExchangeToken - {token}")));
			_controllerlogger.Received(1).Log(LogLevel.Warning, Arg.Is<string>(s => s.Contains($"ExchangeToken - Authentication Failed - {token}")));
		}
		#endregion

		#region AuthRefresh

		[Fact]
		public async Task AuthRefresh_ReturnsSuccess_ReturnedSecureCookies_RememberMe()
		{
			var expected = TestData.AuthResponse1;

			string refreshToken = "thisIsTheOldToken";
			var accessTokenFingerprint = "accessTokenFingerprint";

			_authService.LoginFromRefresh(Arg.Any<string>()).Returns((expected, accessTokenFingerprint));

			var response = await _authController.AuthRefresh(refreshToken);

			await _authService.Received(1).LoginFromRefresh(refreshToken);

			_cookiesService.Received(1).SetCookie(Arg.Any<HttpResponse>(), "AccessTokenFingerprint", accessTokenFingerprint);

			Assert.IsType<OkObjectResult>(response);
			Assert.IsType<(AuthResponse, string)>(((OkObjectResult)response).Value);
			var value = ((OkObjectResult)response).Value;
			value.Should().BeEquivalentTo((expected, accessTokenFingerprint));
		}

		[Fact]
		public async Task AuthRefresh_ReturnsSuccessUsingParameter_ReturnedSecureCookies_RememberMe()
		{
			var expected = TestData.AuthResponse1;

			string refreshToken = "thisIsTheOldToken";
			var accessTokenFingerprint = "accessTokenFingerprint";

			_authService.LoginFromRefresh(Arg.Any<string>()).Returns((expected, accessTokenFingerprint));

			var response = await _authController.AuthRefresh(refreshToken);

			await _authService.Received(1).LoginFromRefresh(refreshToken);

			_cookiesService.Received(1).SetCookie(Arg.Any<HttpResponse>(), "AccessTokenFingerprint", accessTokenFingerprint);

			Assert.IsType<OkObjectResult>(response);
			Assert.IsType<(AuthResponse, string)>(((OkObjectResult)response).Value);
			var value = ((OkObjectResult)response).Value;
			value.Should().BeEquivalentTo((expected, accessTokenFingerprint));
		}

		[Fact]
		public async Task AuthRefresh_MissingRefreshToken_ReturnsBadRequest()
		{
			string refreshToken = "";

			var response = await _authController.AuthRefresh(refreshToken);

			await _authService.DidNotReceive().LoginFromRefresh(Arg.Any<string>());

			_cookiesService.DidNotReceive().SetCookie(Arg.Any<HttpResponse>(), Arg.Any<string>(), Arg.Any<string>());

			Assert.IsType<BadRequestObjectResult>(response);
			Assert.IsType<string>(((BadRequestObjectResult)response).Value);
			var value = ((BadRequestObjectResult)response).Value;
			Assert.Equal("Empty Refresh Token", value);
		}

		[Fact]
		public async Task AuthRefresh_LoginFromRefresh_ReturnsUnauthorized()
		{
			string refreshToken = "thisIsNotAValidRefreshToken";

			_authService.LoginFromRefresh(Arg.Any<string>()).Throws(new AuthenticationFailedException());

			var response = await _authController.AuthRefresh(refreshToken);

			await _authService.Received(1).LoginFromRefresh(refreshToken);

			Assert.IsType<UnauthorizedResult>(response);
		}
		#endregion

		#region InvalidateLogin
		[Fact]
		public void InvalidateLogin_ReturnsSuccess()
		{
			var accountID = 1;
			string accessTokenFingerprint = "accessTokenFingerprint";
			string refreshToken = "refreshToken";

			_cookiesService.GetCookie(Arg.Any<HttpRequest>(), "AccessTokenFingerprint").Returns(accessTokenFingerprint);
			_authService.Authorize(Arg.Any<ClaimsPrincipal>(), Arg.Any<string>()).Returns((accountID, new List<string>()));

			var response = _authController.InvalidateLogin(refreshToken);

			_authService.Received(1).Invalidate(Arg.Any<ClaimsPrincipal>(), refreshToken);

			_cookiesService.Received(1).DeleteCookie(Arg.Any<HttpResponse>(), CookieKeys.AccessTokenFingerprint);

			Assert.IsType<OkResult>(response);
		}

		[Fact]
		public void InvalidateLogin_UnauthorizedJWT_ReturnsUnauthorized()
		{
			string accessTokenFingerprint = "accessTokenFingerprint";

			_cookiesService.GetCookie(Arg.Any<HttpRequest>(), "AccessTokenFingerprint").Returns(accessTokenFingerprint);
			_authService.Authorize(Arg.Any<ClaimsPrincipal>(), Arg.Any<string>()).Throws(new Exception());

			var response = _authController.InvalidateLogin(null);

			_authService.DidNotReceive().Invalidate(Arg.Any<ClaimsPrincipal>(), Arg.Any<string>());
			_cookiesService.DidNotReceive().DeleteCookie(Arg.Any<HttpResponse>(), Arg.Any<string>());

			Assert.IsType<UnauthorizedResult>(response);
		}

		[Fact]
		public void InvalidateLogin_InvalidateException_ReturnsInternalServerError()
		{
			var accountID = 1;
			string accessTokenFingerprint = "accessTokenFingerprint";
			string refreshToken = "refreshToken";

			_cookiesService.GetCookie(Arg.Any<HttpRequest>(), "AccessTokenFingerprint").Returns(accessTokenFingerprint);
			_authService.Authorize(Arg.Any<ClaimsPrincipal>(), Arg.Any<string>()).Returns((accountID, new List<string>()));

			_authService.When(x => x.Invalidate(Arg.Any<ClaimsPrincipal>(), Arg.Any<string>())).Do(x => { throw new Exception(); });

			var response = _authController.InvalidateLogin(refreshToken);

			_authService.Received(1).Invalidate(Arg.Any<ClaimsPrincipal>(), Arg.Any<string>());
			_cookiesService.DidNotReceive().DeleteCookie(Arg.Any<HttpResponse>(), Arg.Any<string>());

			Assert.IsType<StatusCodeResult>(response);
			Assert.Equal((int)HttpStatusCode.InternalServerError, ((StatusCodeResult)response).StatusCode);
		}
		#endregion

		#region CreateJwt
		[Fact]
		public async Task CreateJwt_ReturnsSuccess()
		{
			var request = new JwtRequestFromCode();
			request.Code = "validCode";
			request.State = "validState";

			string federationAccessToken = "FederationAccessToken";
			string federationRefreshToken = "FederationRefreshToken";
			string redirectUrl = "http://redirect-url.com/";

			var federationTokens = new Dictionary<string, string>
			{
				{ "access_token", federationAccessToken },
				{ "refresh_token", federationRefreshToken }
			};
			_jwtSessionService.CreateJwtToken(request.Code, request.State).Returns(federationTokens);
			_jwtSessionService.GetRedirectUrl(request.State).Returns(redirectUrl);

			var response = await _authController.CreateJwt(request);
			Assert.IsType<OkObjectResult>(response);

			var expected = new JwtResponse { jwt = federationTokens, redirect_url = redirectUrl };
			var okResult = response as OkObjectResult;

			Assert.Equal(200, okResult.StatusCode);
			Assert.IsType<JwtResponse>(okResult.Value);
			expected.Should().BeEquivalentTo(okResult.Value);
		}

		[Fact]
		public async Task CreateJwt_NoFederationFound_Unauthorized()
		{
			var request = new JwtRequestFromCode();
			request.Code = "validCode";
			request.State = "invalidState";

			_jwtSessionService.CreateJwtToken(request.Code, request.State).Throws(new NotFoundException());

			var response = await _authController.CreateJwt(request);

			Assert.IsType<UnauthorizedResult>(response);
		}

		[Fact]
		public async Task CreateJwt_OpenIdTokenFailed_Unauthorized()
		{
			var request = new JwtRequestFromCode();
			request.Code = "validCode";
			request.State = "invalidState";

			_jwtSessionService.CreateJwtToken(request.Code, request.State).Throws(new OpenIdException());

			var response = await _authController.CreateJwt(request);

			Assert.IsType<UnauthorizedResult>(response);
		}

		[Fact]
		public async Task CreateJwt_KeyCloakFailed_BadGateway()
		{
			var request = new JwtRequestFromCode();
			request.Code = "validCode";
			request.State = "invalidState";

			_jwtSessionService.CreateJwtToken(request.Code, request.State).Returns(Task.FromResult<Dictionary<string, string>>(null));

			var response = await _authController.CreateJwt(request);

			Assert.IsType<StatusCodeResult>(response);
			Assert.Equal((int)HttpStatusCode.BadGateway, ((StatusCodeResult)response).StatusCode);
		}
		#endregion

		#region FederationUrl
		[Fact]
		public async Task FederationUrl_InvalidModelState_ReturnsBadRequest()
		{
			FederationUrlRequest request = new FederationUrlRequest();
			request.AuthUrl = "SomeUrl";
			request.RedirectUrl = "SomeUrl";
			request.SiteId = (int)SiteTypeEnum.Catalyst;

			_authController.ControllerContext.HttpContext.Request.Headers["UsersAPIToken"] = APIToken.UsersAPIToken;
			_authController.ControllerContext.HttpContext.Request.Headers["Referer"] = "http://url.com";

			_authController.ModelState.AddModelError("Something", "Some Error");
			object response = await _authController.FederationUrl(Arg.Any<string>(), request);

			Assert.IsType<BadRequestObjectResult>(response);
		}

		[Fact]
		public async Task FederationUrl_FederationIsNull_ReturnsNotFoundResult()
		{
			FederationUrlRequest request = new FederationUrlRequest();
			request.AuthUrl = "SomeUrl";
			request.RedirectUrl = "SomeUrl";
			request.SiteId = (int)SiteTypeEnum.Catalyst;

			_authController.ControllerContext.HttpContext.Request.Headers["UsersAPIToken"] = APIToken.UsersAPIToken;
			_authController.ControllerContext.HttpContext.Request.Headers["Referer"] = "http://url.com";

			Federation federation = null;
			_federationService.GetFederationByName(Arg.Any<string>(), Arg.Any<int>()).ReturnsForAnyArgs(federation);
			object response = await _authController.FederationUrl("FederationName", request);

			Assert.IsType<NotFoundResult>(response);
		}

		[Fact]
		public async Task FederationUrl_ReturnsOk()
		{
			FederationUrlRequest request = new FederationUrlRequest();
			request.AuthUrl = "SomeUrl";
			request.RedirectUrl = "SomeUrl";
			request.SiteId = (int)SiteTypeEnum.Catalyst;

			_authController.ControllerContext.HttpContext.Request.Headers["UsersAPIToken"] = APIToken.UsersAPIToken;
			_authController.ControllerContext.HttpContext.Request.Headers["Referer"] = "http://url.com";

			Federation federation = new Federation()
			{
				Name = "ck", 
				SiteId = (int) SiteTypeEnum.Catalyst,
			};

			_federationService.GetFederationByName(Arg.Any<string>(), Arg.Any<int>()).ReturnsForAnyArgs(federation);
			object response = await _authController.FederationUrl("ck", request);

			Assert.IsType<OkObjectResult>(response);
		}

        #endregion

        #region GetAuthUrl
        [Fact]
		public void GetAuthUrl_ReturnsRedirect()
		{
			ALMRequest request = new ALMRequest();
			request.Code = "validCode";
			request.State = "validState";

			string redirectUrl = "http://redirect-url.com/auth/";
			string queryParameters = "?code=validCode&state=validState";

			_jwtSessionService.GetAuthUrl(request.State).Returns(redirectUrl);

			var response = _authController.GetAuthUrl(request);

			Assert.IsType<RedirectResult>(response);
			Assert.Equal(redirectUrl + queryParameters, (response as RedirectResult).Url);
		}

		[Fact]
		public void GetAuthUrl_InvalidCredentials_ReturnsBadRequest()
		{
			ALMRequest request = new ALMRequest();
			request.Code = "validCode";
			request.State = "invalidState";

			_jwtSessionService.GetAuthUrl(request.State).Returns(String.Empty);

			var response = _authController.GetAuthUrl(request);

			Assert.IsType<BadRequestResult>(response);
		}
			
		#endregion

		#region FindFederation
		[Fact]
		public async Task FindFederation_InvalidRequest_ReturnsBadRequest()
		{
			FindFederationRequest request = new FindFederationRequest
			{
				Email = string.Empty
			};

			_authController.ModelState.AddModelError("Something", "Some Error");

			object response = await _authController.FindFederation(request);

			Assert.IsType<BadRequestObjectResult>(response);
			await _federationService.DidNotReceive().GetFederationByEmail(Arg.Any<string>(), Arg.Any<int>());
			_userService.DidNotReceive().Search(Arg.Any<SearchRequestModel>());
		}

		[Fact]
		public async Task FindFederation_FederationIsNull_ReturnsNotFoundResult()
		{
			FindFederationRequest request = new FindFederationRequest
			{
				Email = "email@wiley.com",
				SiteId = (int)SiteTypeEnum.Catalyst
			};

			Federation federation = null;
			_federationService.GetFederationByEmail(Arg.Any<string>(), Arg.Any<int>()).ReturnsForAnyArgs(federation);
			object response = await _authController.FindFederation(request);

			Assert.IsType<NotFoundObjectResult>(response);
			await _federationService.Received(1).GetFederationByEmail(Arg.Any<string>(), Arg.Any<int>());
			_userService.DidNotReceive().Search(Arg.Any<SearchRequestModel>());
		}

		[Fact]
		public async Task FindFederation_ReturnsOkStatusZero()
		{
			FindFederationRequest request = new FindFederationRequest
			{
				Email = "email@wileyqa.com",
				SiteId = (int) SiteTypeEnum.Catalyst,
			};

			Federation federation = new Federation()
			{
				Name = "Wiley"
			};
			_federationService.GetFederationByEmail(Arg.Any<string>(), Arg.Any<int>()).ReturnsForAnyArgs(federation);
			_userService.Search(Arg.Any<SearchRequestModel>()).ReturnsForAnyArgs(new List<SearchResponseModel>());
			object response = await _authController.FindFederation(request);

			Assert.IsType<OkObjectResult>(response);
			Assert.IsType<FindFederationResponse>(((OkObjectResult)response).Value);
			Assert.Equal("Wiley", ((FindFederationResponse)((OkObjectResult)response).Value).FederationName);
			Assert.Equal(0, ((FindFederationResponse)((OkObjectResult)response).Value).Status);
			await _federationService.Received(1).GetFederationByEmail(Arg.Any<string>(), Arg.Any<int>());
			_userService.Received(1).Search(Arg.Any<SearchRequestModel>());
		}

		[Fact]
		public async Task FindFederation_ReturnsOkStatusOne()
		{
			FindFederationRequest request = new FindFederationRequest
			{
				Email = "email@wileyqa.com",
				SiteId = (int)SiteTypeEnum.Catalyst
			};

			Federation federation = new Federation()
			{
				Name = "Wiley"
			};
			var searchResponses = new List<SearchResponseModel>();
			searchResponses.Add(new SearchResponseModel() { FirstName = "x", LastName = "y", UniqueID = "z", UserType = UserTypeEnum.EPICLearner });

			_federationService.GetFederationByEmail(Arg.Any<string>(), Arg.Any<int>()).ReturnsForAnyArgs(federation);
			_userService.Search(Arg.Any<SearchRequestModel>()).ReturnsForAnyArgs(searchResponses);
			object response = await _authController.FindFederation(request);

			Assert.IsType<OkObjectResult>(response);
			Assert.IsType<FindFederationResponse>(((OkObjectResult)response).Value);
			Assert.Equal("Wiley", ((FindFederationResponse)((OkObjectResult)response).Value).FederationName);
			Assert.Equal(1, ((FindFederationResponse)((OkObjectResult)response).Value).Status);
			await _federationService.Received(1).GetFederationByEmail(Arg.Any<string>(), Arg.Any<int>());
			_userService.Received(1).Search(Arg.Any<SearchRequestModel>());
		}

		[Fact]
        public async Task FindFederationSearchNull_ReturnsOk()
        {
            FindFederationRequest request = new FindFederationRequest
            {
                Email = "teste.review@cookiestorie.com",
                SiteId = (int)SiteTypeEnum.Catalyst,
            };

			Federation federation = new Federation
			{
				Name = "CookieStorie",
				SiteId = 2
            };

            _federationService.GetFederationByEmail(Arg.Any<string>(), Arg.Any<int>()).ReturnsForAnyArgs(federation);
            _userService.Search(Arg.Any<SearchRequestModel>()).ReturnsForAnyArgs(new List<SearchResponseModel>());
            object response = await _authController.FindFederation(request);

            Assert.IsType<OkObjectResult>(response);
            Assert.IsType<FindFederationResponse>(((OkObjectResult)response).Value);
            Assert.Equal("CookieStorie", ((FindFederationResponse)((OkObjectResult)response).Value).FederationName);
            Assert.Equal(0, ((FindFederationResponse)((OkObjectResult)response).Value).Status);
            await _federationService.Received(1).GetFederationByEmail(Arg.Any<string>(), Arg.Any<int>());
            _userService.Received(1).Search(Arg.Any<SearchRequestModel>());
        }

        [Fact]
        public async Task FindFederationSearchZeroStatusZero()
        {
            FindFederationRequest request = new FindFederationRequest
            {
                Email = "teste.review@cookiestorie.com",
                SiteId = (int)SiteTypeEnum.Catalyst,
            };

            Federation federation = new Federation
            {
                Name = "CookieStorie",
                SiteId = 2,
				EmailDomain = "teste.review@cookiestorie.us,teste.review@cookiestorie.com"
            };

            var searchResponses = new List<SearchResponseModel>();

            _federationService.GetFederationByEmail(Arg.Any<string>(), Arg.Any<int>()).ReturnsForAnyArgs(federation);
            _userService.Search(Arg.Any<SearchRequestModel>()).ReturnsForAnyArgs(searchResponses);
            object response = await _authController.FindFederation(request);

            Assert.IsType<OkObjectResult>(response);
            Assert.IsType<FindFederationResponse>(((OkObjectResult)response).Value);
            Assert.Equal("CookieStorie", ((FindFederationResponse)((OkObjectResult)response).Value).FederationName);
            Assert.Equal(0, ((FindFederationResponse)((OkObjectResult)response).Value).Status);
            await _federationService.Received(1).GetFederationByEmail(Arg.Any<string>(), Arg.Any<int>());
            _userService.Received(1).Search(Arg.Any<SearchRequestModel>());
        }

        [Fact]
        public async Task FindFederation_InvalidEmail_ReturnsNotFound()
        {
            FindFederationRequest request = new FindFederationRequest
            {
                Email = "invalid.email",
                SiteId = (int)SiteTypeEnum.Catalyst,
            };

            _federationService.GetFederationByEmail(Arg.Any<string>(), Arg.Any<int>()).Returns((Federation)null);
            _userService.Search(Arg.Any<SearchRequestModel>()).ReturnsForAnyArgs(new List<SearchResponseModel>());

            object response = await _authController.FindFederation(request);

            Assert.IsType<NotFoundObjectResult>(response);
            await _federationService.Received(1).GetFederationByEmail(Arg.Any<string>(), Arg.Any<int>());
            _userService.DidNotReceive().Search(Arg.Any<SearchRequestModel>());
        }
        #endregion
    }
}

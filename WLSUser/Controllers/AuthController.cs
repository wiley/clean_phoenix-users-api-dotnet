using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WLS.Log.LoggerTransactionPattern;
using WLSUser.Domain.Constants;
using WLSUser.Domain.Exceptions;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;
using WLSUser.Services;
using WLSUser.Services.Interfaces;

namespace WLSUser.Controllers
{
    [Route("api/v{version:apiVersion}/auth")]
	[ApiVersion("1.0")]
	[ApiController]
	public class AuthController : ControllerBase
	{

		private readonly IAuthService _authService;
		private readonly ICookiesService _cookiesService;
		private readonly ILogger<AuthController> _logger;
		private readonly IFederationService _federationService;
		private readonly IJwtSessionService _jwtSessionService;
		private readonly IUserService _userService;
		private readonly ILoggerStateFactory _loggerStateFactory;

		public AuthController(
			IAuthService authService,
			ICookiesService cookiesService,
			ILogger<AuthController> logger,
			IFederationService federationService,
			IJwtSessionService jwtSessionService,
			IUserService userService,
			ILoggerStateFactory loggerStateFactory)
		{
			_authService = authService;
			_cookiesService = cookiesService;
			_logger = logger;
			_federationService = federationService;
			_jwtSessionService = jwtSessionService;
			_userService = userService;
			_loggerStateFactory = loggerStateFactory;
		}

		[HttpPost]
		[ProducesResponseType(typeof(AuthResponse), 200)]
		[ProducesResponseType(400)]
		[ProducesResponseType(401)]
		[ProducesResponseType(500)]
		public async Task<IActionResult> Auth([FromBody]AuthRequest request)
		{
			using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
			{
				if (request == null)
				{
					_logger.LogWarning("Auth - Bad Request Object");
					return BadRequest();
				}

				_logger.LogInformation("Auth - {Username}", request.Username);

				try
				{
					if (!ModelState.IsValid)
					{
						_logger.LogWarning("Auth - Invalid Model State - {Username}", request.Username);
						return BadRequest(ModelState);
					}

					AuthResponse authResponse;
					string accessTokenFingerprint;

					try
					{
						(authResponse, accessTokenFingerprint) = await _authService.Login(request);
					}
					catch (AuthenticationFailedException)
					{
						_logger.LogWarning("Auth - Authentication Failed - {Username}", request.Username);
						return Unauthorized();
					}

					_cookiesService.SetCookie(Response, "AccessTokenFingerprint", accessTokenFingerprint);

					return Ok((authResponse, accessTokenFingerprint));
				}

				catch (Exception e)
				{
					_logger.LogError(e, "Auth Failed - {Username}", request.Username);
					return StatusCode((int)HttpStatusCode.InternalServerError);
				}
			}
		}

		[HttpPost("token/exchange")]
		[ProducesResponseType(typeof(AuthResponse), 200)]
		[ProducesResponseType(400)]
		[ProducesResponseType(401)]
		[ProducesResponseType(500)]
		public async Task<IActionResult> ExchangeToken([FromBody] string token)
		{
			using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
			{
				if (string.IsNullOrWhiteSpace(token))
				{
					_logger.LogWarning("ExchangeToken - Empty Token Received");
					return BadRequest("Empty Token");
				}

				try
				{
					_logger.LogInformation("ExchangeToken - {token}", token);
					try
					{
						var (authResponse, accessTokenFingerprint) = await _authService.ExchangeToken(token);
						if (authResponse != null)
						{
							_logger.LogInformation("ExchangeToken - Authentication Success - {exchangeToken} > {accesToken}", token, authResponse.AccessToken);
							return Ok((authResponse, accessTokenFingerprint));
						}
						return NotFound();
					}
					catch (AuthenticationFailedException)
					{
						_logger.LogWarning("ExchangeToken - Authentication Failed - {token}", token);
						return Unauthorized();
					}
				}
				catch (Exception e)
				{
					_logger.LogError(e, "ExchangeToken Failed - {token}", token);
					return StatusCode((int)HttpStatusCode.InternalServerError);
				}
			}
		}

		[HttpPost("refresh")]
		[ProducesResponseType(typeof(AuthResponse), 200)]
		[ProducesResponseType(400)]
		[ProducesResponseType(401)]
		[ProducesResponseType(500)]
        [Authorize(AuthenticationSchemes = "Custom")]
        public async Task<IActionResult> AuthRefresh([FromBody] string refreshToken)
		{
			AuthResponse authResponse;
			string accessTokenFingerprint;

			using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
			{
				try
				{
					_logger.LogInformation("AuthRefresh - {secureRefresh}", refreshToken);

					if (string.IsNullOrEmpty(refreshToken))
					{
						_logger.LogWarning("AuthRefresh - Received empty refresh token");
						return BadRequest("Empty Refresh Token");
					}

					try
					{
						(authResponse, accessTokenFingerprint) = await _authService.LoginFromRefresh(refreshToken);
						if (authResponse != null)
							_logger.LogInformation("AuthRefresh - Authentication Success - {refreshToken} > {newAccessToken}", refreshToken, authResponse.AccessToken);
					}
					catch (AuthenticationFailedException)
					{
						_logger.LogWarning("AuthRefresh - Authentication Failed - {secureRefresh}", refreshToken);
						return Unauthorized();
					}

					_cookiesService.SetCookie(Response, "AccessTokenFingerprint", accessTokenFingerprint);
					return Ok(( authResponse, accessTokenFingerprint));
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "AuthRefresh Failed");
					return StatusCode((int)HttpStatusCode.InternalServerError);
				}
			}
		}

		[HttpPost("invalidate")]
		[ProducesResponseType(200)]
		[ProducesResponseType(400)]
		[ProducesResponseType(401)]
		[ProducesResponseType(404)]
        [Authorize(AuthenticationSchemes = "Custom")]
        public IActionResult InvalidateLogin([FromBody] string refreshToken)
		{
			using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
			{
				int accountID;

				try
				{
					string accessTokenFingerprint =  _cookiesService.GetCookie(Request, CookieKeys.AccessTokenFingerprint);
					(accountID, _) = _authService.Authorize(User, accessTokenFingerprint);

					_logger.LogInformation("Auth Invalidate - {AccountID}", accountID);
				}
				catch
				{
					_logger.LogWarning("Auth Invalidate - Unauthorized JWT");
					return Unauthorized();
				}

				try
				{
					_authService.Invalidate(User, refreshToken);

					_logger.LogInformation("Auth Invalidate - Access/Refresh Token Invalidated - {AccountID}", accountID);

					_cookiesService.DeleteCookie(Response, CookieKeys.AccessTokenFingerprint);

					_logger.LogInformation("Auth Invalidate - Secure Cookies Wiped - {AccountID}", accountID);
					return Ok();
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Auth Invalidate Failed - {AccountID}", accountID);
					return StatusCode((int)HttpStatusCode.InternalServerError);
				}
			}
		}

		[HttpPost("{federationName}/url")]
		[ProducesResponseType(typeof(SsoFederationUrlResponse), 200)]
		[ProducesResponseType(400)]
		[ProducesResponseType(401)]
		[ProducesResponseType(404)]
		public async Task<IActionResult> FederationUrl([Required] string federationName, FederationUrlRequest federationRequest)
		{
			Guid transactionID = LoggingHelper.GetTransactionID(Request.Headers);
			string stateId = transactionID.ToString();
			var loggerState = new Dictionary<string, object>
			{
				["TransactionID"] = transactionID.ToString()
			};

			using (_logger.BeginScope(loggerState))
			{
				try
				{
					if (!ModelState.IsValid)
					{
						_logger.LogWarning("FederationUrl - Invalid Model State - {federationName}", federationName);
						return BadRequest(ModelState);
					}

					var federation = await _federationService.GetFederationByName(federationName, federationRequest.SiteId);
					if (federation == null)
					{
						return NotFound();
					}

					_jwtSessionService.SetFederationName(stateId, federationName);
					_jwtSessionService.SetRedirectUrl(stateId, federationRequest.RedirectUrl);
					_jwtSessionService.SetAuthUrl(stateId, federationRequest.AuthUrl);

					return Ok(_federationService.GetFederationUrl(federation, stateId));
				}
				catch (Exception e)
				{
					_logger.LogError(e, "FederationUrl Failed - {federationName}", federationName);
					return BadRequest(new LoginResponseModel() { Status = "Unknown" });
				}
			}
		}

		[HttpPost("jwt")]
		[ProducesResponseType(typeof(JwtResponse), 200)]
		[ProducesResponseType(400)]
		public async Task<IActionResult> CreateJwt(JwtRequestFromCode request)
		{
			using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
			{
				try
				{
					string redirectUrl = _jwtSessionService.GetRedirectUrl(request.State);
					Dictionary<string, string> jwt = await _jwtSessionService.CreateJwtToken(request.Code, request.State);
					if (jwt == null)
						throw new Exception();

					return Ok((new JwtResponse { jwt = jwt, redirect_url = redirectUrl }));
				}
				catch(NotFoundException e)
				{
					_logger.LogWarning(e, "Create JWT - Federation not found");

					return Unauthorized();
				}
				catch(OpenIdException e)
				{
					_logger.LogWarning(e, "Create JWT - Failed to create OpenId token");

					return Unauthorized();
				}
				catch (Exception e)
				{
					_logger.LogError(e, "Failed to create JWT - {Code}", request.Code);

					return StatusCode((int)HttpStatusCode.BadGateway);
				}
			}
		}

		[HttpGet("authUrl")]
		[ProducesResponseType(302)]
		[ProducesResponseType(400)]
		public IActionResult GetAuthUrl([FromQuery] ALMRequest request)
		{
			using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
			{
				string url = _jwtSessionService.GetAuthUrl(request.State);
				if (String.IsNullOrWhiteSpace(url))
				{
					_logger.LogError(String.Format("Failed to retrieve authUrl from State {0}", request.State));

					return BadRequest();
				}

				var queryString = new Dictionary<string, string>
				{
					{ "code", request.Code },
					{ "state", request.State }
				};

				Uri authUrl = new Uri(QueryHelpers.AddQueryString(url, queryString));

				return Redirect(authUrl.AbsoluteUri);
			}
		}

		[HttpPost("federation")]
		[ProducesResponseType(typeof(FindFederationResponse), 200)]
		[ProducesResponseType(400)]
		[ProducesResponseType(404)]
		public async Task<IActionResult> FindFederation([FromBody] FindFederationRequest request)
		{
			using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
			{
				try
				{
					if (request == null)
                    {
						_logger.LogWarning("FindFederation - Invalid Request");
						return BadRequest();
                    }
					if (!ModelState.IsValid)
					{
						_logger.LogWarning("FindFederation - Invalid Model State - {Email}", request.Email);
						return BadRequest(ModelState);
					}

					var federation = await _federationService.GetFederationByEmail(request.Email, request.SiteId);
					if (federation == null)
					{
						return NotFound($"No federations for this email domain: {request.Email}");
					}

					int status = 0;
					UserTypeEnum userType;
					switch (request.SiteId)
                    {
						case (int)SiteTypeEnum.Catalyst:
							userType = UserTypeEnum.EPICLearner;
							break;
						//TODO: when adding other Federation Site types, 
						default:
							userType = UserTypeEnum.EPICLearner;
							break;
                    }

					IEnumerable<SearchResponseModel> searchResults = _userService.Search(new SearchRequestModel() { Username = request.Email, UserType = userType });
					if (searchResults != null)
						status = searchResults.Count();

					var response = new FindFederationResponse
					{ 
						FederationName = federation.Name,
						Status = status
					};
					return Ok(response);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "FindFederation - Request Failed - {Email}", request.Email);
					return StatusCode((int)HttpStatusCode.InternalServerError);
				}
			}
		}
	}
}

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WLSUser.Domain.Constants;
using WLSUser.Domain.Exceptions;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;
using WLSUser.Services.Interfaces;

namespace WLSUser.Services.Authentication
{
    public class AuthService : IAuthService
	{
		private readonly IUserService _userService = null;
		private readonly IRedisService _redisService;
		private readonly IJwtFactory _jwtFactory = null;
		private readonly ILogger<AuthService> _logger;

		private const string _statusConnected = "Connected";

		public AuthService(IUserService userService, IRedisService redisService, IJwtFactory jwtFactory, ILogger<AuthService> logger)
		{
			_userService = userService;
			_redisService = redisService;
			_jwtFactory = jwtFactory;
			_logger = logger;
		}

		public async Task<(AuthResponse authResponse, string accessTokenFingerprint)> Login(AuthRequest request)
		{
			var response = new AuthResponse();
			string accessTokenFingerprint = null;

			var loginResponse = _userService.Login(request.Username, request.Password, request.SiteType, request.UserType, true);

			if (loginResponse.Status != _statusConnected)
			{
				_logger.LogInformation("AuthService - Login - Not connected");
				throw new AuthenticationFailedException();
			}
			else
			{
				_logger.LogInformation("AuthService - Login - Connected");
				(response, accessTokenFingerprint) = await CreateAuthResponse(loginResponse, request.PersistToken, TokenTypeEnum.AccessToken);
			}

			return (response, accessTokenFingerprint);
		}

		public async Task<(AuthResponse authResponse, string accessTokenFingerprint)> ExchangeToken(string token)
		{
			AuthResponse authResponse;
			string accessTokenFingerprint;
			ClaimsPrincipal claimsPrincipal;

			try
			{
				claimsPrincipal = _jwtFactory.DecodeToken(token);
			}
			catch (Exception ex)
			{
				throw new AuthenticationFailedException($"Invalid Exchange Token - {ex.Message}");
			}

			var siteName = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == JwtClaimIdentifiers.SiteType)?.Value;
			if (siteName == null || !Enum.TryParse<SiteTypeEnum>(siteName, true, out SiteTypeEnum siteType))
				throw new AuthenticationFailedException("Invalid Exchange Token - Site");

			var userName = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == JwtClaimIdentifiers.UserName)?.Value;
			if (userName == null)
				throw new AuthenticationFailedException("Invalid Exchange Token - UserName");

			var userTypeName = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == JwtClaimIdentifiers.UserType)?.Value;
			if (userTypeName == null || !Enum.TryParse<UserTypeEnum>(userTypeName, true, out UserTypeEnum userType))
				throw new AuthenticationFailedException("Invalid Exchange Token - UserType");

			LoginResponse loginResponse = _userService.Login(userName, null, siteType, userType, false);

			if (loginResponse.Status != _statusConnected)
			{
				_logger.LogInformation("AuthService - ExchangeToken - Not connected");
				throw new AuthenticationFailedException();
			}
			else
			{
				_logger.LogInformation("AuthService - ExchangeToken - Connected");
				(authResponse, accessTokenFingerprint) = await CreateAuthResponse(loginResponse, true, TokenTypeEnum.ExchangeToken);
			}
			return (authResponse, accessTokenFingerprint);
		}

		public async Task<(AuthResponse authResponse, string tokenFingerprint)> LoginFromRefresh(string refreshToken)
		{
			var response = new AuthResponse();
			string accessTokenFingerprint = null;
			ClaimsPrincipal claimsPrincipal = null;

			try
			{
				claimsPrincipal = _jwtFactory.DecodeToken(refreshToken);
			}
			catch
			{
				throw new AuthenticationFailedException();
			}

			string refreshTokenIdentifier = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "jti").Value;

			if (!IsRefreshTokenIdentifierValid(refreshTokenIdentifier))
				throw new AuthenticationFailedException();

			string uniqueID = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == JwtClaimIdentifiers.Id).Value;

			LoginResponse loginResponse = _userService.LoginV2Refresh(claimsPrincipal.Identity.Name, uniqueID);

			(response, accessTokenFingerprint) = await CreateAuthResponse(loginResponse, true, TokenTypeEnum.AccessToken);

			InvalidateRefreshTokenIdentifier(refreshTokenIdentifier);

			return (response, accessTokenFingerprint);
		}

		public (int accountID, List<string> roleTypes) Authorize(ClaimsPrincipal user, string accessTokenFingerprint)
		{
			IEnumerable<Claim> claims = user.Claims;

			string accessTokenFingerprintFromJWT = claims.FirstOrDefault(c => c.Type == "fgp").Value;
			string accessTokenIdentifier = claims.FirstOrDefault(c => c.Type == "jti").Value;

			if (!ValidateTokenFingerprint(accessTokenFingerprintFromJWT, accessTokenFingerprint))
			{
				_logger.LogWarning("AuthService - Authorize - Invalid Access Token Fingerprint - {accessTokenIdentifier}", accessTokenIdentifier);
				throw new UnauthorizedAccessException();
			}

			if (!IsAccessTokenIdentifierValid(accessTokenIdentifier))
			{
				_logger.LogWarning("AuthService - Authorize - The Access Token identifier has been invalidated - {accessTokenIdentifier}", accessTokenIdentifier);
				throw new UnauthorizedAccessException();
			}

			string uniqueID = claims.FirstOrDefault(c => c.Type == JwtClaimIdentifiers.Id).Value;
			var userUniqueID = new UserUniqueID(uniqueID);

			List<string> roleTypes = claims.Where(c => c.Type == JwtClaimIdentifiers.Rol).Select(c => c.Value).ToList();

			return (userUniqueID.AccountID, roleTypes);
		}

		private bool ValidateTokenFingerprint(string fingerprintFromAccessToken, string fingerprintFromCookie)
		{
			return true;
			// using (SHA256 hash = SHA256Managed.Create()) {
			// 	fingerprintFromCookie = String.Concat(hash
			// 											.ComputeHash(Encoding.UTF8.GetBytes(fingerprintFromCookie))
			// 											.Select(item => item.ToString("x2")));
			// }

			// return (fingerprintFromAccessToken != fingerprintFromCookie) ? false : true;
		}

		private void InvalidateAccessTokenIdentifier(string accessTokenIdentifier, DateTime expiryDate)
		{
			int expiry = (int)(expiryDate - DateTime.UtcNow).TotalSeconds;

			_redisService.SetString(expiry, $"accessToken_invalidated_{accessTokenIdentifier}", "");
		}

		private bool IsAccessTokenIdentifierValid(string accessTokenIdentifier)
		{
			return !_redisService.KeyExists($"accessToken_invalidated_{accessTokenIdentifier}");
		}

		private void InvalidateRefreshTokenIdentifier(string refreshTokenIdentifier)
		{
			_redisService.ClearKey($"refreshToken_{refreshTokenIdentifier}");
		}

		private bool IsRefreshTokenIdentifierValid(string refreshTokenIdentifier)
		{
			return _redisService.KeyExists($"refreshToken_{refreshTokenIdentifier}");
		}

		public void Invalidate(ClaimsPrincipal user, string refreshToken)
		{
			IEnumerable<Claim> claims = user.Claims;
			string accessTokenIdentifier = claims.FirstOrDefault(c => c.Type == "jti").Value;
			string expiryDateString = claims.FirstOrDefault(c => c.Type == "exp").Value;
			DateTime expiryDate = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(expiryDateString)).UtcDateTime;
			InvalidateAccessTokenIdentifier(accessTokenIdentifier, expiryDate);

			if (!string.IsNullOrEmpty(refreshToken))
			{
				IEnumerable<Claim> refreshTokenClaims = _jwtFactory.DecodeToken(refreshToken).Claims;
				string refreshTokenIdentifier = refreshTokenClaims.FirstOrDefault(c => c.Type == "jti").Value;
				InvalidateRefreshTokenIdentifier(refreshTokenIdentifier);
			}
		}

		public async Task<(AuthResponse authResponse, string accessTokenFingerprint)>
		CreateAuthResponse(LoginResponse loginResponse, bool createRefreshToken = false, TokenTypeEnum expirationTokenType = TokenTypeEnum.AccessToken)
		{
			var (accessJwt, encodedAccessJwt, accessTokenFingerprint) = await CreateAccessToken(loginResponse, expirationTokenType);

			var (refreshJwt, encodedRefreshJwt) = createRefreshToken ? await CreateRefreshToken(loginResponse) : (null, "");

			return (new AuthResponse
			{
				AccessToken = encodedAccessJwt,
				Expires = accessJwt.ValidTo.ToUniversalTime(),
				RefreshToken = createRefreshToken ? encodedRefreshJwt : "",
				RefreshExpires = createRefreshToken ? refreshJwt.ValidTo.ToUniversalTime() : DateTime.MinValue.ToUniversalTime()
			},
			accessTokenFingerprint);
		}

		private async Task<(JwtSecurityToken jwt, string encodedJwt, string accessTokenFingerprint)> CreateAccessToken(LoginResponse loginResponse, TokenTypeEnum expirationTokenType = TokenTypeEnum.AccessToken)
		{
			string accessTokenFingerprint = Guid.NewGuid().ToString(); // Random meaningless value

			(JwtSecurityToken jwt, string encodedJwt) = await _jwtFactory.GenerateEncodedToken(loginResponse, expirationTokenType, accessTokenFingerprint);

			return (jwt, encodedJwt, accessTokenFingerprint);
		}

		private async Task<(JwtSecurityToken jwt, string encodedJwt)> CreateRefreshToken(LoginResponse loginResponse)
		{
			(JwtSecurityToken jwt, string encodedJwt) = await _jwtFactory.GenerateEncodedToken(loginResponse, TokenTypeEnum.RefreshToken);

			int secondsToExpire = (int)(jwt.ValidTo.ToUniversalTime() - DateTime.UtcNow).TotalSeconds;
			string cacheKey = $"refreshToken_{jwt.Id}";
			_redisService.SetString(secondsToExpire, cacheKey, "");

			return (jwt, encodedJwt);
		}
	}
}

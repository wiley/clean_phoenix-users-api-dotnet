using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;

namespace WLSUser.Services.Interfaces
{
    public interface IAuthService
    {
    Task<(AuthResponse authResponse, string accessTokenFingerprint)> Login(AuthRequest request);

		Task<(AuthResponse authResponse, string tokenFingerprint)> LoginFromRefresh(string refreshToken);

		(int accountID, List<string> roleTypes) Authorize(ClaimsPrincipal user, string accessTokenFingerprint);

		void Invalidate(ClaimsPrincipal user, string refreshToken);

		Task<(AuthResponse authResponse, string accessTokenFingerprint)>
		CreateAuthResponse(LoginResponse loginResponse, bool createRefreshToken = false, TokenTypeEnum expirationTokenType = TokenTypeEnum.AccessToken);

		Task<(AuthResponse authResponse, string accessTokenFingerprint)> ExchangeToken(string token);
	}
}

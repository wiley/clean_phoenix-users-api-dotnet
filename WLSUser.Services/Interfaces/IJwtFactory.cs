using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;

namespace WLSUser.Services.Interfaces
{
    public interface IJwtFactory
    {
        Task<(JwtSecurityToken jwt, string encodedJwt)> GenerateEncodedToken(LoginResponse loginResponse, TokenTypeEnum expirationTokenType, string tokenFingerprint = null);
        ClaimsPrincipal DecodeToken(string token);
    }
}

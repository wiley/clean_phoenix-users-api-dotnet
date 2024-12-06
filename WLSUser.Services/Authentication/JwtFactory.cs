using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using WLSUser.Domain.Constants;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;
using WLSUser.Services.Interfaces;

namespace WLSUser.Services.Authentication
{
    public class JwtFactory : IJwtFactory
	{
		private readonly JwtIssuerOptions _jwtOptions;
		private readonly JwtExchangeOptions _jwtExchangeOptions;
		private readonly JwtExpirations _jwtExpirations;

        public JwtFactory(IOptions<JwtIssuerOptions> jwtOptions, IOptions<JwtExchangeOptions> jwtExchangeOptions, IOptions<JwtExpirations> jwtExpirations)
        {
            _jwtOptions = jwtOptions.Value;
			_jwtExchangeOptions = jwtExchangeOptions.Value;
			_jwtExpirations = jwtExpirations.Value;
            ThrowIfInvalidOptions(_jwtOptions);
        }

        public async Task<(JwtSecurityToken jwt, string encodedJwt)> GenerateEncodedToken(LoginResponse loginResponse, TokenTypeEnum expirationTokenType, string tokenFingerprint = null)
		{
			var tokenTypeSettings = _jwtExpirations.Tokens.FirstOrDefault(t => t.Type == expirationTokenType.ToString());
			_jwtOptions.ValidFor = TimeSpan.FromSeconds(tokenTypeSettings.TotalSeconds);
			var jti = await _jwtOptions.JtiGenerator();

			var claimsIdentity = new ClaimsIdentity(new GenericIdentity(loginResponse.UserName, "Token"), new Claim[]
			{
				new Claim(JwtRegisteredClaimNames.Jti, jti),
				new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_jwtOptions.IssuedAt).ToString(), ClaimValueTypes.Integer64),
				new Claim(JwtClaimIdentifiers.Id, loginResponse.UniqueID),
				new Claim(JwtClaimIdentifiers.UserType, ((int)loginResponse.UserType).ToString())
			});

			foreach (var role in loginResponse.Roles)
            {
				claimsIdentity.AddClaim(new Claim(JwtClaimIdentifiers.Rol, role));
            }

			if (!string.IsNullOrEmpty(tokenFingerprint))
			{
				using (SHA256 hash = SHA256Managed.Create())
				{
					tokenFingerprint = String.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(tokenFingerprint)).Select(item => item.ToString("x2")));
				}
				claimsIdentity.AddClaim(new Claim("fgp", tokenFingerprint));
			}

			// Create the JWT security token and encode it. 
			var jwt = new JwtSecurityToken(
				issuer: _jwtOptions.Issuer,
				audience: _jwtOptions.Audience,
				claims: claimsIdentity.Claims,
				notBefore: _jwtOptions.NotBefore,
				expires: _jwtOptions.Expiration,
				signingCredentials: _jwtOptions.SigningCredentials);
			
			var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
			return (jwt, encodedJwt);
		}

		public ClaimsPrincipal DecodeToken(string token)
		{
			var tokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuer = true,
				ValidIssuers = new List<string> { _jwtOptions.Issuer, _jwtExchangeOptions.Issuer },

				ValidateAudience = true,
				ValidAudiences = new List<string> { _jwtOptions.Audience, _jwtExchangeOptions.Audience },

				ValidateIssuerSigningKey = true,
				IssuerSigningKeys = new List<SecurityKey> { _jwtOptions.SigningCredentials.Key, _jwtExchangeOptions.SigningCredentials.Key },

				RequireExpirationTime = false,
				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero
			};

			var tokenHandler = new JwtSecurityTokenHandler();

			return tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
		}

		/// <returns>Date converted to seconds since Unix epoch (Jan 1, 1970, midnight UTC).</returns> 
		private static long ToUnixEpochDate(DateTime date)
		  => (long)Math.Round((date.ToUniversalTime() -
							   new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
							  .TotalSeconds);

		private static void ThrowIfInvalidOptions(JwtIssuerOptions options)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));

			if (options.ValidFor <= TimeSpan.Zero)
			{
				throw new ArgumentException("Must be a non-zero TimeSpan.", nameof(JwtIssuerOptions.ValidFor));
			}

			if (options.SigningCredentials == null)
			{
				throw new ArgumentNullException(nameof(JwtIssuerOptions.SigningCredentials));
			}

			if (options.JtiGenerator == null)
			{
				throw new ArgumentNullException(nameof(JwtIssuerOptions.JtiGenerator));
			}
		}
	}
}

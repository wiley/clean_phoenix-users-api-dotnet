using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;

namespace WLSUser.Tests
{
    public static class TestData
	{

		public static AuthResponse AuthResponse1 = new AuthResponse
		{
			AccessToken = "ABCDEFG",
			Expires = DateTime.Now,
			RefreshToken = "ZYXWVUT",
			RefreshExpires = DateTime.Now.AddHours(72)
		};

		public static JwtExpirations JWTExpirations => new JwtExpirations
		{
			Tokens = new List<JwtExpirations.Token>
			{
				new JwtExpirations.Token
				{
					Type = "AccessToken",
					Minutes = 15
				},
				new JwtExpirations.Token
				{
					Type = "RefreshToken",
					Hours = 72
				}
			}
		};

		private static ClaimsPrincipal ClaimsPrincipal(List<Claim> claims)
		{
			claims.Add(new Claim("jti", "accessTokenIdentifier"));
			claims.Add(new Claim("fgp", "c1b5ca3cd27ea4c7a0fbe77aa2020aff00575fb493739ae8b20ef823efd3708c")); //SHA256 encrypted string "accessTokenFingerprint"
			claims.Add(new Claim("exp", "1337"));

			return new ClaimsPrincipal(new ClaimsIdentity(claims.ToArray(), "mockClaim"));
		}

		public static string UniqueIdEpicLearner()
		{
			return "epic:singleton:learner:1234";
		}

		public static List<RoleAccessReference> RoleAccessReferenceList()
		{
			var roleAccessReferences = new List<RoleAccessReference> { RoleAccessReference() };
			return roleAccessReferences;
		}

		public static RoleAccessReference RoleAccessReference()
        {
			return new RoleAccessReference
			{
				RoleType = new RoleType { RoleTypeID = 1, BrandID = 1, RoleName = "Role Type Test" },
				AccessType = new AccessType { AccessTypeID = 1, AccessTypeName = "Access Type Test" },
				UserRoleAccessList = new List<UserRoleAccess> { new UserRoleAccess { UserRoleID = 1, AccessTypeID = 1, AccessRefID = 1234, GrantedBy = 1, Created = DateTime.Parse("1/1/2020") } }
			};
		}

		public static ClaimsPrincipal ClaimsRefresh()
        {
			List<Claim> claims = new List<Claim>
			{
				new Claim("id", TestData.UniqueIdEpicLearner())
			};

			return TestData.ClaimsPrincipal(claims);
		}

		public static ClaimsPrincipal ClaimsLearner()
		{
			List<Claim> claims = new List<Claim>
			{
				new Claim("id", TestData.UniqueIdEpicLearner())
			};

			return TestData.ClaimsPrincipal(claims);
		}

		public static LoginResponse LoginResponse()
        {
			var loginResponse = new LoginResponse()
			{
				UniqueID = "epic:singleton:learner:1234",
				UserName = "user@test.com",
				UserType = UserTypeEnum.Any,
				FirstName = "Test",
				LastName = "User",
				Status = "Connected"
			};
			return loginResponse;
        }

		public static UserModel user1 = new()
		{
			UserID = 9000001,
			Username = "unique@some.com",
			FirstName = "unis",
			LastName = "nique",
			UserType = UserTypeEnum.EPICLearner,
			Status = 2
		};

	}
}
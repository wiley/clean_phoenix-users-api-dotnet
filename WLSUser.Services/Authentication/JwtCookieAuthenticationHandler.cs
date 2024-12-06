using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using WLSUser.Domain.Constants;
using WLSUser.Services.Interfaces;

namespace WLSUser.Services.Authentication
{
    public class JwtSchemeOptions : AuthenticationSchemeOptions { }

	public class JwtCookieAuthenticationHandler : AuthenticationHandler<JwtSchemeOptions>
	{
		private readonly IAuthService _authService;
		private readonly ICookiesService _cookiesService;
		private readonly ILogger<JwtCookieAuthenticationHandler> _logger;

		public JwtCookieAuthenticationHandler(IOptionsMonitor<JwtSchemeOptions> options, ILoggerFactory loggerFactory, UrlEncoder encoder, ISystemClock clock,
											  IAuthService authService, ICookiesService cookiesService,
											  ILogger<JwtCookieAuthenticationHandler> logger) : base(options, loggerFactory, encoder, clock)
		{
			_authService = authService;
			_cookiesService = cookiesService;
			_logger = logger;
		}

		protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			try
			{
				var endpoint = Context.GetEndpoint();
				if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
					return AuthenticateResult.NoResult();

				if (!Request.Headers.ContainsKey("Authorization"))
					return AuthenticateResult.Fail("Missing Authorization Header");

				string accessTokenFingerprint = _cookiesService.GetCookie(Context.Request, CookieKeys.AccessTokenFingerprint);
				(_, var roles) = _authService.Authorize(Context.User, accessTokenFingerprint);
				var claims = Context.User.Claims;
				var identity = new ClaimsIdentity(claims, Scheme.Name);
				var principal = new GenericPrincipal(identity, roles.ToArray());
				var ticket = new AuthenticationTicket(principal, Scheme.Name);

				return AuthenticateResult.Success(ticket);
			}
			catch (Exception)
			{
				return AuthenticateResult.Fail("Unauthorized");
			}
		}
	}
}

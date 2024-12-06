using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using WLS.Log.LoggerTransactionPattern;
using WLSUser.Domain.Models;
using WLSUser.Services.Interfaces;

namespace WLSUser.Controllers
{

    [Route("api/v{version:apiVersion}/sso")]
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [ApiController]
    public class SsoController : Controller
    {
        private readonly IFederationService _federationService;
        private readonly ILogger<SsoController> _logger;
        private readonly ILoggerStateFactory _loggerStateFactory;

        public SsoController(IFederationService federationService, ILogger<SsoController> logger, ILoggerStateFactory loggerStateFactory)
        {
            _logger = logger;
            _federationService = federationService;
            _loggerStateFactory = loggerStateFactory;
        }

        [HttpGet("{federationName}/{siteId?}")]
        public async Task<IActionResult> Login([Required] string federationName, [FromQuery][StringLength(255)] string state, int? siteId = (int)SiteTypeEnum.Catalyst)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("Login - Invalid Model State - {federationName}", federationName);
                        return BadRequest(ModelState);
                    }

                    var federation = await _federationService.GetFederationByName(federationName, siteId ?? (int)SiteTypeEnum.Catalyst);
                    if (federation == null)
                    {
                        var url = Request.GetTypedHeaders().Referer;
                        return Redirect(url.AbsoluteUri);
                    }

                    string stateKey = await _federationService.StoreStateInformation(state);

                    string federationScope = !string.IsNullOrEmpty(federation.Scope) ? federation.Scope : FederationConstants.DefaultScope;

                    var queryString = new Dictionary<string, string>
                    {
                        { "client_id", federation.OpenIdClientId },
                        { "response_type", "code" },
                        { "state", stateKey },
                        { "scope", federationScope },
                        { "redirect_uri", federation.RedirectUrl }
                    };

                    if (!string.IsNullOrWhiteSpace(federation.AlmFederationName))
                    {
                        queryString.Add("kc_idp_hint", federation.AlmFederationName);
                    }

                    var loginUrl = new Uri(QueryHelpers.AddQueryString(federation.OpenIdAuthInitUrl, queryString));

                    return Redirect(loginUrl.AbsoluteUri);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Login Failed - {federationName}", federationName);
                    return BadRequest(new LoginResponseModel() { Status = "Unknown" });
                }
            }
        }     
    }
}

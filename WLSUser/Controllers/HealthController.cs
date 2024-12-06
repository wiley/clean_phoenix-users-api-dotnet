using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WLSUser.Domain.Models;
using WLSUser.Services.Interfaces;

namespace WLSUser.Controllers
{
    [Consumes("application/json")]
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [ApiController]
    public class HealthController : Controller
    {
        private IHealthService _healthCheckService { get; set; }


        public HealthController(IHealthService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        [HttpGet]
        [Obsolete]
        [Route("api/v{version:apiVersion}/Health")]
        public IActionResult HealthCheckLegacy()
        {
            //this method is to be removed, currently keep for legacy purposes during transition
            HealthResponseLegacyModel response = new HealthResponseLegacyModel();

            //TODO: Check status of one or more dependencies
            //i.e. EPIC Forgot Password API call...

            response.Status = "Healthy";
            response.Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            return Ok(response);
        }

        [HttpGet]
        [ProducesResponseType(typeof(HealthResponseModel), 200)]
        [ProducesResponseType(typeof(HealthResponseExtendedModel), 503)]
        [Route("/Healthz")]
        public IActionResult HealthCheck()
        {
            if (_healthCheckService.PerformHealthCheck())
            {
                return Ok(new HealthResponseModel { Status = HealthResults.OK });
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new HealthResponseExtendedModel { Status = HealthResults.Fail, Message = "Unable to connect to database" });
        }

        [HttpGet]
        [ProducesResponseType(typeof(Dictionary<string, string>), 200)]
        [ProducesResponseType(typeof(Dictionary<string, string>), 503)]
        [Route("/Healthz/Dependencies")]
        public async Task<IActionResult> HealthCheckDependenciesAsync()
        {
            Dictionary<string, string> result = await _healthCheckService.VerifyDependencies();

            if (_healthCheckService.CheckDependenciesResult(result))
            {
                return Ok(result);
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        }
    }
}
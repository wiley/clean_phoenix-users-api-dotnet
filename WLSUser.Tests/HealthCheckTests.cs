using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WLS.Monitoring.HealthCheck.Interfaces;
using WLSUser.Controllers;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Interfaces;
using WLSUser.Services.Interfaces;
using Xunit;

namespace WLSUser.Tests
{
    public class HealthCheckTests
    {
        private readonly HealthController _controller = null;
        private readonly IAppConfig _config;
        private readonly IDbHealthCheck _dbHealthCheck;
        private readonly ILearnerEmailAPI _learnerApi;
        private readonly IHealthService _healthService;

        public HealthCheckTests()
        {
            _config = Substitute.For<IAppConfig>();
            _dbHealthCheck = Substitute.For<IDbHealthCheck>();
            _learnerApi = Substitute.For<ILearnerEmailAPI>();
            ILogger<Startup> logger = NullLogger<Startup>.Instance;
            _healthService = Substitute.For<IHealthService>();
            _controller = new HealthController(_healthService);
        }

        #region HealthCheck

        [Fact]
        public void HealthCheck_ReturnsSuccess()
        {
            //Arrange
            string expectedStatus = HealthResults.OK;
            _healthService.PerformHealthCheck().Returns(true);

            //Act
            var response = _controller.HealthCheck();

            //Assert
            Assert.IsType<OkObjectResult>(response);
            var value = ((OkObjectResult)response).Value;
            var healthResponse = ((HealthResponseModel)value);
            Assert.Equal(0, string.Compare(expectedStatus, healthResponse.Status));
        }

        [Fact]
        public void HealthCheck_ReturnsUnavailable_DueBadConnection()
        {
            //Arrange
            string expectedStatus = HealthResults.Fail;
            string expectedMessage = "Unable to connect to database";
            _healthService.PerformHealthCheck().Returns(false);

            //Act
            var response = _controller.HealthCheck();

            //Assert
            Assert.IsType<ObjectResult>(response);
            Assert.Equal(503, ((ObjectResult)response).StatusCode);
            var value = ((ObjectResult)response).Value;
            var healthResponse = ((HealthResponseExtendedModel)value);
            Assert.Equal(0, string.Compare(expectedStatus, healthResponse.Status));
            Assert.Equal(0, string.Compare(expectedMessage, healthResponse.Message));
        }

        #endregion

        #region HealthCheckDependencies

        [Fact]
        public async void HealthCheckDependencies_ReturnsSuccess()
        {
            //Arrange
            string expectedApiKey = DependenciesTypes.LearnerEmailAPI;
            string expectedMySqlKey = DependenciesTypes.MySql;
            string expectedOk = HealthResults.OK;
            Dictionary<string, string> verifyResult = new Dictionary<string, string>
            {
                { expectedApiKey, expectedOk },
                { expectedMySqlKey, expectedOk }
            };
            _healthService.VerifyDependencies().Returns(verifyResult);
            _healthService.CheckDependenciesResult(Arg.Any<Dictionary<string, string>>()).Returns(true);

            //Act
            var response = await _controller.HealthCheckDependenciesAsync();

            //Assert
            Assert.IsType<OkObjectResult>(response);
            var value = ((OkObjectResult)response).Value;
            var finalResult = (Dictionary<string, string>)value;
            Assert.True(finalResult.ContainsKey(expectedApiKey));
            Assert.True(finalResult.ContainsKey(expectedMySqlKey));
            Assert.True(finalResult.ContainsValue(expectedOk));
        }

        [Fact]
        public async void HealthCheckDependencies_ReturnsUnavailable_DueUnavailable()
        {
            //Arrange
            string expectedKey = DependenciesTypes.LearnerEmailAPI;
            string expectedUnavailable = HealthResults.Unavailable;
            Dictionary<string, string> verifyResult = new Dictionary<string, string>
            {
                { expectedKey, expectedUnavailable }
            };
            _healthService.VerifyDependencies().Returns(verifyResult);
            _healthService.CheckDependenciesResult(Arg.Any<Dictionary<string, string>>()).Returns(false);

            //Act
            var response = await _controller.HealthCheckDependenciesAsync();

            //Assert
            Assert.IsType<ObjectResult>(response);
            Assert.Equal(503, ((ObjectResult)response).StatusCode);
            var value = ((ObjectResult)response).Value;
            var finalResult = (Dictionary<string, string>)value;
            Assert.True(finalResult.ContainsKey(expectedKey));
            Assert.True(finalResult.ContainsValue(expectedUnavailable));
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WLS.Monitoring.HealthCheck.Interfaces;
using WLS.Monitoring.HealthCheck.Models;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Interfaces;
using WLSUser.Services;
using WLSUser.Services.Interfaces;
using Xunit;

namespace WLSUser.Tests.Services
{
    public class HealthServiceTests
    {
        private readonly IAppConfig _config;
        private readonly IDbHealthCheck _dbHealthCheck;
        private readonly ILearnerEmailAPI _learnerApi;
        private readonly IHealthService _healthService;
        private readonly ILogger<HealthService> _logger;
        private readonly IRedisService _redisService;

        public HealthServiceTests()
        {
            _config = Substitute.For<IAppConfig>();
            _config.ConnectionString = "fakeConnectionString";
            _dbHealthCheck = Substitute.For<IDbHealthCheck>();
            _learnerApi = Substitute.For<ILearnerEmailAPI>();
            _logger = Substitute.For<ILogger<HealthService>>();
            _redisService = Substitute.For<IRedisService>();
            _healthService = new HealthService(_config, _learnerApi, _logger, _dbHealthCheck, _redisService);
        }

        #region PerformHealthCheck

        [Fact]
        public void PerformHealthCheck_ReturnsTrue()
        {
            //Arrange
            DbHealthCheckResponse mockedReturn = new DbHealthCheckResponse(true, "");
            _dbHealthCheck.MySqlConnectionTest(Arg.Any<string>()).Returns(mockedReturn);
            _redisService.IsConnected().Returns(true);

            //Act
            var result = _healthService.PerformHealthCheck();

            //Assert
            _dbHealthCheck.Received(1).MySqlConnectionTest(_config.ConnectionString);
            _redisService.Received(1).IsConnected();
            Assert.True(result);
        }

        [Fact]
        public void PerformHealthCheck_ReturnsFalseDueBadMySqlConnection()
        {
            //Arrange
            DbHealthCheckResponse mockedReturn = new DbHealthCheckResponse(false, "");
            _dbHealthCheck.MySqlConnectionTest(Arg.Any<string>()).Returns(mockedReturn);
            _redisService.IsConnected().Returns(true);

            //Act
            var result = _healthService.PerformHealthCheck();

            //Assert
            _dbHealthCheck.Received(1).MySqlConnectionTest(_config.ConnectionString);
            _redisService.Received(1).IsConnected();
            Assert.False(result);
        }

        [Fact]
        public void PerformHealthCheck_ReturnsFalseDueBadRedisConnection()
        {
            //Arrange
            DbHealthCheckResponse mockedReturn = new DbHealthCheckResponse(true, "");
            _dbHealthCheck.MySqlConnectionTest(Arg.Any<string>()).Returns(mockedReturn);
            _redisService.IsConnected().Returns(false);

            //Act
            var result = _healthService.PerformHealthCheck();

            //Assert
            _dbHealthCheck.Received(1).MySqlConnectionTest(_config.ConnectionString);
            _redisService.Received(1).IsConnected();
            Assert.False(result);
        }

        [Fact]
        public void PerformHealthCheck_ReturnsFalseMySqlException()
        {
            //Arrange
            _dbHealthCheck.MySqlConnectionTest(Arg.Any<string>()).Throws(new Exception());
            _redisService.IsConnected().Returns(true);

            //Act
            var result = _healthService.PerformHealthCheck();

            //Assert
            _dbHealthCheck.Received(1).MySqlConnectionTest(_config.ConnectionString);
            _redisService.Received(1).IsConnected();
            Assert.False(result);
        }

        #endregion

        #region VerifyDependencies

        [Fact]
        public async void VerifyDependencies_ReturnsOk()
        {
            //Arrange
            var expected = new Dictionary<string, string>
            {
                { DependenciesTypes.MySql, HealthResults.OK },
                { DependenciesTypes.LearnerEmailAPI, HealthResults.OK },
                { DependenciesTypes.RedisConnection, HealthResults.OK },
                { DependenciesTypes.RedisReadWrite, HealthResults.OK }
            };
            DbHealthCheckResponse mockedReturn = new DbHealthCheckResponse(true, "");
            _dbHealthCheck.MySqlConnectionTest(Arg.Any<string>()).Returns(mockedReturn);
            _learnerApi.RequestHealthCheck().Returns(true);
            _redisService.IsConnected().Returns(true);
            _redisService.GetString(RedisTestConstants.Key).Returns(RedisTestConstants.Value);

            //Act
            Dictionary<string, string> result = await _healthService.VerifyDependencies();

            //Assert
            _dbHealthCheck.Received(1).MySqlConnectionTest(_config.ConnectionString);
            await _learnerApi.Received(1).RequestHealthCheck();
            _redisService.Received(1).IsConnected();
            _redisService.Received(1).SetString(RedisTestConstants.ExpirySeconds, RedisTestConstants.Key, RedisTestConstants.Value);
            _redisService.Received(1).GetString(RedisTestConstants.Key);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async void VerifyDependencies_ReturnsUnavailableDueMySql()
        {
            //Arrange
            var expected = new Dictionary<string, string>
            {
                { DependenciesTypes.MySql, HealthResults.Unavailable },
                { DependenciesTypes.LearnerEmailAPI, HealthResults.OK },
                { DependenciesTypes.RedisConnection, HealthResults.OK },
                { DependenciesTypes.RedisReadWrite, HealthResults.OK }
            };
            DbHealthCheckResponse mockedReturn = new DbHealthCheckResponse(false, "");
            _dbHealthCheck.MySqlConnectionTest(Arg.Any<string>()).Returns(mockedReturn);
            _learnerApi.RequestHealthCheck().Returns(true);
            _redisService.IsConnected().Returns(true);
            _redisService.GetString(RedisTestConstants.Key).Returns(RedisTestConstants.Value);

            //Act
            Dictionary<string, string> result = await _healthService.VerifyDependencies();

            //Assert
            _dbHealthCheck.Received(1).MySqlConnectionTest(_config.ConnectionString);
            await _learnerApi.Received(1).RequestHealthCheck();
            _redisService.Received(1).IsConnected();
            _redisService.Received(1).SetString(RedisTestConstants.ExpirySeconds, RedisTestConstants.Key, RedisTestConstants.Value);
            _redisService.Received(1).GetString(RedisTestConstants.Key);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async void VerifyDependencies_ReturnsUnavailableDueLeanerApi()
        {
            //Arrange
            var expected = new Dictionary<string, string>
            {
                { DependenciesTypes.MySql, HealthResults.OK },
                { DependenciesTypes.LearnerEmailAPI, HealthResults.Unavailable },
                { DependenciesTypes.RedisConnection, HealthResults.OK },
                { DependenciesTypes.RedisReadWrite, HealthResults.OK }
            };
            DbHealthCheckResponse mockedReturn = new DbHealthCheckResponse(true, "");
            _dbHealthCheck.MySqlConnectionTest(Arg.Any<string>()).Returns(mockedReturn);
            _learnerApi.RequestHealthCheck().Returns(false);
            _redisService.IsConnected().Returns(true);
            _redisService.GetString(RedisTestConstants.Key).Returns(RedisTestConstants.Value);

            //Act
            Dictionary<string, string> result = await _healthService.VerifyDependencies();

            //Assert
            _dbHealthCheck.Received(1).MySqlConnectionTest(_config.ConnectionString);
            await _learnerApi.Received(1).RequestHealthCheck();
            _redisService.Received(1).IsConnected();
            _redisService.Received(1).SetString(RedisTestConstants.ExpirySeconds, RedisTestConstants.Key, RedisTestConstants.Value);
            _redisService.Received(1).GetString(RedisTestConstants.Key);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async void VerifyDependencies_ReturnsUnavailableDueRedisConnection()
        {
            //Arrange
            var expected = new Dictionary<string, string>
            {
                { DependenciesTypes.MySql, HealthResults.OK },
                { DependenciesTypes.LearnerEmailAPI, HealthResults.OK },
                { DependenciesTypes.RedisConnection, HealthResults.Unavailable },
                { DependenciesTypes.RedisReadWrite, HealthResults.Unavailable }
            };
            DbHealthCheckResponse mockedReturn = new DbHealthCheckResponse(true, "");
            _dbHealthCheck.MySqlConnectionTest(Arg.Any<string>()).Returns(mockedReturn);
            _learnerApi.RequestHealthCheck().Returns(true);
            _redisService.IsConnected().Returns(false);

            //Act
            Dictionary<string, string> result = await _healthService.VerifyDependencies();

            //Assert
            _dbHealthCheck.Received(1).MySqlConnectionTest(_config.ConnectionString);
            await _learnerApi.Received(1).RequestHealthCheck();
            _redisService.Received(1).IsConnected();
            _redisService.DidNotReceive().SetString(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>());
            _redisService.DidNotReceive().GetString(Arg.Any<string>());
            Assert.Equal(expected, result);
        }

        [Fact]
        public async void VerifyDependencies_ReturnsUnavailableDueRedisReadWrite()
        {
            //Arrange
            var expected = new Dictionary<string, string>
            {
                { DependenciesTypes.MySql, HealthResults.OK },
                { DependenciesTypes.LearnerEmailAPI, HealthResults.OK },
                { DependenciesTypes.RedisConnection, HealthResults.OK },
                { DependenciesTypes.RedisReadWrite, HealthResults.Unavailable }
            };
            DbHealthCheckResponse mockedReturn = new DbHealthCheckResponse(true, "");
            _dbHealthCheck.MySqlConnectionTest(Arg.Any<string>()).Returns(mockedReturn);
            _learnerApi.RequestHealthCheck().Returns(true);
            _redisService.IsConnected().Returns(true);
            _redisService.GetString(RedisTestConstants.Key).Returns("");

            //Act
            Dictionary<string, string> result = await _healthService.VerifyDependencies();

            //Assert
            _dbHealthCheck.Received(1).MySqlConnectionTest(_config.ConnectionString);
            await _learnerApi.Received(1).RequestHealthCheck();
            _redisService.Received(1).IsConnected();
            _redisService.Received(1).SetString(RedisTestConstants.ExpirySeconds, RedisTestConstants.Key, RedisTestConstants.Value);
            _redisService.Received(1).GetString(RedisTestConstants.Key);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async void VerifyDependencies_ReturnsUnavailableDueExceptionMySql()
        {
            //Arrange
            var expected = new Dictionary<string, string>
            {
                { DependenciesTypes.MySql, HealthResults.Unavailable },
                { DependenciesTypes.LearnerEmailAPI, HealthResults.OK },
                { DependenciesTypes.RedisConnection, HealthResults.OK },
                { DependenciesTypes.RedisReadWrite, HealthResults.OK }
            };
            _dbHealthCheck.MySqlConnectionTest(Arg.Any<string>()).Throws(new Exception());
            _learnerApi.RequestHealthCheck().Returns(true);
            _redisService.IsConnected().Returns(true);
            _redisService.GetString(RedisTestConstants.Key).Returns(RedisTestConstants.Value);

            //Act
            Dictionary<string, string> result = await _healthService.VerifyDependencies();

            //Assert
            _dbHealthCheck.Received(1).MySqlConnectionTest(_config.ConnectionString);
            await _learnerApi.Received(1).RequestHealthCheck();
            _redisService.Received(1).IsConnected();
            _redisService.Received(1).SetString(RedisTestConstants.ExpirySeconds, RedisTestConstants.Key, RedisTestConstants.Value);
            _redisService.Received(1).GetString(RedisTestConstants.Key);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async void VerifyDependencies_ReturnsUnavailableDueExceptionLearner()
        {
            //Arrange
            var expected = new Dictionary<string, string>
            {
                { DependenciesTypes.MySql, HealthResults.OK },
                { DependenciesTypes.LearnerEmailAPI, HealthResults.Unavailable },
                { DependenciesTypes.RedisConnection, HealthResults.OK },
                { DependenciesTypes.RedisReadWrite, HealthResults.OK }
            };
            DbHealthCheckResponse mockedReturn = new DbHealthCheckResponse(true, "");
            _dbHealthCheck.MySqlConnectionTest(Arg.Any<string>()).Returns(mockedReturn);
            _learnerApi.RequestHealthCheck().Throws(new Exception());
            _redisService.IsConnected().Returns(true);
            _redisService.GetString(RedisTestConstants.Key).Returns(RedisTestConstants.Value);

            //Act
            Dictionary<string, string> result = await _healthService.VerifyDependencies();

            //Assert
            _dbHealthCheck.Received(1).MySqlConnectionTest(_config.ConnectionString);
            await _learnerApi.Received(1).RequestHealthCheck();
            _redisService.Received(1).IsConnected();
            _redisService.Received(1).SetString(RedisTestConstants.ExpirySeconds, RedisTestConstants.Key, RedisTestConstants.Value);
            _redisService.Received(1).GetString(RedisTestConstants.Key);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async void VerifyDependencies_ReturnsUnavailableDueExceptionRedisConnection()
        {
            //Arrange
            var expected = new Dictionary<string, string>
            {
                { DependenciesTypes.MySql, HealthResults.OK },
                { DependenciesTypes.LearnerEmailAPI, HealthResults.OK },
                { DependenciesTypes.RedisConnection, HealthResults.Unavailable },
                { DependenciesTypes.RedisReadWrite, HealthResults.Unavailable }
            };
            DbHealthCheckResponse mockedReturn = new DbHealthCheckResponse(true, "");
            _dbHealthCheck.MySqlConnectionTest(Arg.Any<string>()).Returns(mockedReturn);
            _learnerApi.RequestHealthCheck().Returns(true);
            _redisService.IsConnected().Throws(new Exception());

            //Act
            Dictionary<string, string> result = await _healthService.VerifyDependencies();

            //Assert
            _dbHealthCheck.Received(1).MySqlConnectionTest(_config.ConnectionString);
            await _learnerApi.Received(1).RequestHealthCheck();
            _redisService.Received(1).IsConnected();
            _redisService.DidNotReceive().SetString(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>());
            _redisService.DidNotReceive().GetString(Arg.Any<string>());
            Assert.Equal(expected, result);
        }
        [Fact]
        public async void VerifyDependencies_ReturnsUnavailableDueExceptionRedisReadWrite()
        {
            //Arrange
            var expected = new Dictionary<string, string>
            {
                { DependenciesTypes.MySql, HealthResults.OK },
                { DependenciesTypes.LearnerEmailAPI, HealthResults.OK },
                { DependenciesTypes.RedisConnection, HealthResults.OK },
                { DependenciesTypes.RedisReadWrite, HealthResults.Unavailable }
            };
            DbHealthCheckResponse mockedReturn = new DbHealthCheckResponse(true, "");
            _dbHealthCheck.MySqlConnectionTest(Arg.Any<string>()).Returns(mockedReturn);
            _learnerApi.RequestHealthCheck().Returns(true);
            _redisService.IsConnected().Returns(true);
            _redisService.GetString(RedisTestConstants.Key).Throws(new Exception());

            //Act
            Dictionary<string, string> result = await _healthService.VerifyDependencies();

            //Assert
            _dbHealthCheck.Received(1).MySqlConnectionTest(_config.ConnectionString);
            await _learnerApi.Received(1).RequestHealthCheck();
            _redisService.Received(1).IsConnected();
            _redisService.Received(1).SetString(RedisTestConstants.ExpirySeconds, RedisTestConstants.Key, RedisTestConstants.Value);
            _redisService.Received(1).GetString(RedisTestConstants.Key);
            Assert.Equal(expected, result);
        }

        #endregion

        #region CheckDependencies

        [Fact]
        public void CheckDependenciesResult_ReturnsTrue()
        {
            //Arrange
            Dictionary<string, string> verifyResult = new Dictionary<string, string>
            {
                { DependenciesTypes.MySql, HealthResults.OK },
                { DependenciesTypes.LearnerEmailAPI, HealthResults.OK }
            };

            //Act
            var result = _healthService.CheckDependenciesResult(verifyResult);

            //Assert
            Assert.True(result);
        }

        [Fact]
        public void CheckDependenciesResult_ReturnsFalse()
        {
            //Arrange
            Dictionary<string, string> verifyResult = new Dictionary<string, string>
            {
                { DependenciesTypes.MySql, HealthResults.OK },
                { DependenciesTypes.LearnerEmailAPI, HealthResults.Unavailable }
            };

            //Act
            var result = _healthService.CheckDependenciesResult(verifyResult);

            //Assert
            Assert.False(result);
        }

        #endregion
    }
}
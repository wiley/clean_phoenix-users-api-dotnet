using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WLS.Monitoring.HealthCheck.Interfaces;
using WLS.Monitoring.HealthCheck.Models;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Interfaces;
using WLSUser.Services.Interfaces;

namespace WLSUser.Services
{
    public class HealthService : IHealthService
    {
        private readonly IAppConfig _configuration;
        private readonly IDbHealthCheck _dbHealthCheck;
        private readonly ILearnerEmailAPI _learnerEmailApi;
        private readonly ILogger<HealthService> _logger;
        private readonly IRedisService _redisService;

        public HealthService(IAppConfig configuration, ILearnerEmailAPI learnerEmailApi,
            ILogger<HealthService> logger, IDbHealthCheck dbHealthCheck, IRedisService redisService)
        {
            _configuration = configuration;
            _learnerEmailApi = learnerEmailApi;
            _logger = logger;
            _dbHealthCheck = dbHealthCheck;
            _redisService = redisService;
        }

        public bool PerformHealthCheck()
        {
            var result = new Dictionary<string, string>();
            CheckMySqlConnection(result);
            CheckRedisConnection(result);
            return CheckDependenciesResult(result);
        }

        public async Task<Dictionary<string, string>> VerifyDependencies()
        {
            var result = new Dictionary<string, string>();
            CheckMySqlConnection(result);
            await CheckLeanerEmailApiAsync(result);

            var isRedisConnected = CheckRedisConnection(result);
            CheckRedisReadWrite(result, isRedisConnected);

            return result;
        }

        private void CheckMySqlConnection(Dictionary<string, string> result)
        {
            try
            {
                var connectionString = _configuration.ConnectionString;
                DbHealthCheckResponse mySqlCheck = _dbHealthCheck.MySqlConnectionTest(connectionString);

                if (mySqlCheck.SuccessfulConnection)
                {
                    result.Add(DependenciesTypes.MySql, HealthResults.OK);
                }
                else
                {
                    result.Add(DependenciesTypes.MySql, HealthResults.Unavailable);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to reach Mysql database, {0}", ex.Message);
                result.Add(DependenciesTypes.MySql, HealthResults.Unavailable);
            }
        }

        private async Task CheckLeanerEmailApiAsync(Dictionary<string, string> result)
        {
            try
            {
                bool learnerApiHealth = await _learnerEmailApi.RequestHealthCheck();
                if (learnerApiHealth)
                {
                    result.Add(DependenciesTypes.LearnerEmailAPI, HealthResults.OK);
                }
                else
                {
                    result.Add(DependenciesTypes.LearnerEmailAPI, HealthResults.Unavailable);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to reach LeanerEmailAPI , {0}", ex.Message);
                result.Add(DependenciesTypes.LearnerEmailAPI, HealthResults.Unavailable);
            }
        }

        private bool CheckRedisConnection(Dictionary<string, string> result)
        {
            try
            {
                bool redisServiceIsConnected = _redisService.IsConnected();
                result.Add(DependenciesTypes.RedisConnection, redisServiceIsConnected ? HealthResults.OK : HealthResults.Unavailable);
                return redisServiceIsConnected;
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to reach RedisCache , {0}", ex.Message);
                result.Add(DependenciesTypes.RedisConnection, HealthResults.Unavailable);
                return false;
            }
        }

        private void CheckRedisReadWrite(Dictionary<string, string> result, bool isConnected)
        {
            try
            {
                if (!isConnected)
                {
                    result.Add(DependenciesTypes.RedisReadWrite, HealthResults.Unavailable);
                    return;
                }

                _redisService.SetString(RedisTestConstants.ExpirySeconds, RedisTestConstants.Key, RedisTestConstants.Value);
                var readValue = _redisService.GetString(RedisTestConstants.Key);
                result.Add(DependenciesTypes.RedisReadWrite, readValue == RedisTestConstants.Value ? HealthResults.OK : HealthResults.Unavailable);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to read and/or write from RedisCache , {0}", ex.Message);
                result.Add(DependenciesTypes.RedisReadWrite, HealthResults.Unavailable);
            }
        }

        public bool CheckDependenciesResult(Dictionary<string, string> results)
        {
            bool finalResult = true;
            if (results == null || results.Count == 0)
            {
                return false;
            }

            foreach (KeyValuePair<string, string> keyValue in results)
            {
                if (!keyValue.Value.Equals(HealthResults.OK))
                {
                    finalResult = false;
                    break;
                }
            }

            return finalResult;
        }
    }
}
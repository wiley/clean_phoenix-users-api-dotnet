using FluentAssertions;

using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System;
using System.Collections.Generic;
using WLSUser.Services;

using Xunit;
using WLSUser.Tests.Util;
using WLSUser.Domain.Models;
using WLSUser.Services.Interfaces;

namespace WLSUser.Tests.Services
{
    public class RedisServiceTests
    {
        private readonly MockLogger<RedisService> _logger;
        private readonly RedisService _redisServiceValid;
        private readonly RedisService _redisServiceInvalidIP;
        private IRedisService _redisServiceKeys;

        public RedisServiceTests()
        {
            _logger = Substitute.For<MockLogger<RedisService>>();

            var options1 = new RedisServiceOptions()
            {
                Connection = "redis.dev.darwin.private.wiley.host:6379",
                UnitTest = true,
                CacheSettings = new List<RedisServiceOptions.CacheSetting>()
                {
                    new RedisServiceOptions.CacheSetting()
                    {
                        Key = "x",
                        Minutes = 5
                    }
                }
            };

            var options2 = new RedisServiceOptions()
            {
                Connection = "aaaaaaaaaaaaa",
                UnitTest = true,
                CacheSettings = new List<RedisServiceOptions.CacheSetting>()
                {
                    new RedisServiceOptions.CacheSetting()
                    {
                        Key = "x",
                        Minutes = 5
                    }
                }
            };

            _redisServiceValid = new RedisService(Options.Create<RedisServiceOptions>(options1), _logger);
            _redisServiceInvalidIP = new RedisService(Options.Create<RedisServiceOptions>(options1), _logger);
        }


        [Fact]
        public void CacheExpireTime_valid()
        {
            var expected = DateTime.Now.AddMinutes(5);
            var response = _redisServiceValid.CacheExpireTime("x");

            response.Should().BeCloseTo(expected,1000);
        }

        [Fact]
        public void SetString_InvaidIP_LogsError()
        {
            string baseKey = "a";
            string cacheKey = baseKey + "bcd";

            string logMessage = String.Format("RedisService - SetString Failed - {0}", cacheKey);

            _redisServiceInvalidIP.SetString(baseKey, cacheKey, "xyz");
            _logger.Received(1).Log(LogLevel.Error, Arg.Is<string>(s => s.Contains(logMessage)));
        }

        [Fact]
        public void GetString_InvaidIP_LogsError()
        {
            string cacheKey = "abcd";

            string logMessage = String.Format("RedisService - GetString Failed - {0}", cacheKey);

            var response = _redisServiceInvalidIP.GetString(cacheKey);

            _logger.Received(1).Log(LogLevel.Error, Arg.Is<string>(s => s.Contains(logMessage)));
            Assert.Equal("", response);

        }

        [Fact]
        public void GetStrings_InvaidIP_LogsError()
        {
            List<string> cacheKeys = new List<string>() { "abcd" };

            string logMessage = String.Format("RedisService - GetStrings Failed - {0}", cacheKeys.Count);

            var response = _redisServiceInvalidIP.GetStrings(cacheKeys);

            _logger.Received(1).Log(LogLevel.Error, Arg.Is<string>(s => s.Contains(logMessage)));
            Assert.Null(response);
        }

        [Fact]
        public void SetStrings_InvaidIP_LogsError()
        {
            string baseKey = "a";
            Dictionary<string, string> cacheKeys = new Dictionary<string, string>() { { "abcd", "" } };

            string logMessage = String.Format("RedisService - SetStrings Failed - {0}", cacheKeys.Count);

            _redisServiceInvalidIP.SetStrings(baseKey, cacheKeys);

            _logger.Received(1).Log(LogLevel.Error, Arg.Is<string>(s => s.Contains(logMessage)));
        }

        [Fact]
        public void ClearKey_InvaidIP_LogsError()
        {
            string cacheKey = "abcd";

            string logMessage = String.Format("RedisService - ClearKey Failed - {0}", cacheKey);

            _redisServiceInvalidIP.ClearKey(cacheKey);

            _logger.Received(1).Log(LogLevel.Error, Arg.Is<string>(s => s.Contains(logMessage)));
        }

    }
}

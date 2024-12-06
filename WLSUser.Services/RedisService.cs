using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using WLSUser.Domain.Models;
using WLSUser.Services.Interfaces;

namespace WLSUser.Services
{
    public class RedisService : IRedisService
    {
        private readonly IOptions<RedisServiceOptions> _options;
        private readonly ILogger<RedisService> _logger;
        private ConnectionMultiplexer _connection;
        private static IDatabase _cacheDatabase;

        public RedisService(IOptions<RedisServiceOptions> options, ILogger<RedisService> logger)
        {
            _options = options;
            _logger = logger;

            if (!_options.Value.UnitTest)
            {
                try
                {
                    _connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(options.Value.Connection)).Value;
                    _cacheDatabase = _connection.GetDatabase();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RedisService - Connection Failed - {0}", options.Value.Connection);
                }
            }
        }


        public DateTime CacheExpireTime(string baseKey)
        {
            var setting = _options.Value.CacheSettings.Find(u => u.Key == baseKey);

            int seconds = (setting != null) ? setting.TotalSeconds : 60;

            return DateTime.Now.AddSeconds(seconds);
        }

        public TimeSpan CacheExpireTimeSpan(string baseKey)
        {
            var setting = _options.Value.CacheSettings.Find(u => u.Key == baseKey);

            int seconds = (setting != null) ? setting.TotalSeconds : 60;

            return new TimeSpan(0, 0, seconds);
        }

        public bool KeyExists(string cacheKey)
        {
            try
            {
                return _cacheDatabase.KeyExists(cacheKey);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "RedisService - KeyExists Failed - {CacheKey}", cacheKey);
                return false;
            }
        }

        public void SetString(string baseKey, string cacheKey, string value)
        {
            try
            {
                _cacheDatabase.StringSet(cacheKey, value, CacheExpireTimeSpan(baseKey));

            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "RedisService - SetString Failed - {CacheKey}", cacheKey);
            }
        }

        public void SetString(int secondsToExpire, string cacheKey, string value)
        {
            try
            {
                _cacheDatabase.StringSet(cacheKey, value, new TimeSpan(0, 0, secondsToExpire));
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "RedisService - SetString Failed - {CacheKey}", cacheKey);
            }
        }

        public void SetExpiry(int secondsToExpire, string cacheKey)
        {
            try
            {
                _cacheDatabase.KeyExpireAsync(cacheKey, new TimeSpan(0, 0, secondsToExpire));
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "RedisService - SetExpiry Failed - {CacheKey}", cacheKey);
            }
        }

        public string GetString(string cacheKey)
        {
            try
            {
                return _cacheDatabase.StringGet(cacheKey);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "RedisService - GetString Failed - {CacheKey}", cacheKey);
                return "";
            }
        }

        public T Get<T>(string cacheKey)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(GetString(cacheKey));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RedisService - Get<T> Failed - {CacheKey}", cacheKey);
                return default;
            }
        }

        public IDictionary<string, string> GetStrings(List<string> cacheKeys)
        {
            try
            {
                RedisKey[] keys = cacheKeys.Select(key => (RedisKey)key).ToArray();
                var strings = _cacheDatabase.StringGet(keys);
                return Enumerable.Range(0, keys.Length).ToDictionary(i => cacheKeys[i], i => (string)strings[i]);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "RedisService - GetStrings Failed - {KeyCount}", (cacheKeys != null) ? cacheKeys.Count : -1);
                return null;
            }
        }

        public void SetStrings(string baseKey, Dictionary<string, string> cacheItems)
        {
            try
            {
                TimeSpan expireTime = CacheExpireTimeSpan(baseKey);
                var tran = _cacheDatabase.CreateTransaction();

                foreach (var entry in cacheItems)
                {
                    tran.AddCondition(Condition.KeyNotExists(entry.Key));
                    tran.StringSetAsync(entry.Key, entry.Value, expireTime);
                }

                bool committed = tran.Execute();

                if (!committed)
                {
                    _logger.LogWarning("RedisService - SetStrings - Transaction Execution False - {KeyCount}", (cacheItems != null) ? cacheItems.Count : -1);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "RedisService - SetStrings Failed - {KeyCount}", (cacheItems != null) ? cacheItems.Count : -1);
            }
        }

        public void ClearKey(string cacheKey)
        {
            try
            {
                _cacheDatabase.KeyDelete(cacheKey);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "RedisService - ClearKey Failed - {CacheKey}", cacheKey);
            }
        }

        public bool IsConnected()
        {
            return _cacheDatabase.IsConnected("IsConnected");
        }
    }
}

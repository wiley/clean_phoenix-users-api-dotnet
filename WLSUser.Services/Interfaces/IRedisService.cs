using StackExchange.Redis;
using System.Collections.Generic;

namespace WLSUser.Services.Interfaces
{
    public interface IRedisService
    {
        bool KeyExists(string cacheKey);
        void SetString(string baseKey, string cacheKey, string value);
        void SetString(int secondsToExpire, string cacheKey, string value);
        string GetString(string cacheKey);
        IDictionary<string, string> GetStrings(List<string> cacheKeys);
        void SetStrings(string baseKey, Dictionary<string, string> cacheItems);
        void ClearKey(string cacheKey);
        void SetExpiry(int secondsToExpire, string cacheKey);
        T Get<T>(string cacheKey);
        bool IsConnected();
    }
}

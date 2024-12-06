using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System;
using WLSUser.Services.Interfaces;
using WLSUser.Domain.Models.Authentication;

namespace WLSUser.Services.Authentication
{
    public class CookiesService : ICookiesService
    {
        private readonly IOptions<CookiesOptions> _options;
        private readonly ILogger<CookiesService> _logger;

        public CookiesService(IOptions<CookiesOptions> options, ILogger<CookiesService> logger)
        {
            _options = options;
            _logger = logger;
        }

        public void SetCookie(HttpResponse response, string cookieKey, string value, bool httpOnly = true, bool secure = true,
                                SameSiteMode sameSite = SameSiteMode.Strict, DateTimeOffset? expiration = null)
        {
            var cookieSettings = _options.Value.Cookies.FirstOrDefault(c => c.Name == cookieKey);

            if (cookieSettings != null)
            {
                cookieKey = cookieSettings.Key;
                httpOnly = cookieSettings.HttpOnly;
                secure = cookieSettings.Secure;
                sameSite = cookieSettings.SameSite;
                expiration = DateTimeOffset.UtcNow.AddSeconds(cookieSettings.TotalSeconds);
            }

            response.Cookies.Append(cookieKey, value, new CookieOptions() {
                HttpOnly = httpOnly,
                Secure = secure,
                SameSite = sameSite,
                Expires = expiration
            });
        }

        public void DeleteCookie(HttpResponse response, string cookieKey, bool httpOnly = true, bool secure = true, SameSiteMode sameSite = SameSiteMode.Strict)
        {
            var cookieSettings = _options.Value.Cookies.FirstOrDefault(c => c.Name == cookieKey);

            if (cookieSettings != null)
            {
                cookieKey = cookieSettings.Key;
                httpOnly = cookieSettings.HttpOnly;
                secure = cookieSettings.Secure;
                sameSite = cookieSettings.SameSite;
            }

            response.Cookies.Delete(cookieKey, new CookieOptions() {
                HttpOnly = httpOnly,
                Secure = secure,
                SameSite = sameSite
            });
        }

        public string GetCookie(HttpRequest request, string cookieKey)
        {
            var cookieSettings = _options.Value.Cookies.FirstOrDefault(c => c.Name == cookieKey);

            if (cookieSettings != null)
                cookieKey = cookieSettings.Key;

            return request.Cookies.FirstOrDefault(c => c.Key == cookieKey).Value;
        }
    }
}

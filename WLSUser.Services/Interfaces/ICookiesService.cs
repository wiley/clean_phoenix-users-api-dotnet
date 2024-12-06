using Microsoft.AspNetCore.Http;
using System;

namespace WLSUser.Services.Interfaces
{
    public interface ICookiesService
    {
        void SetCookie(HttpResponse response, string cookieKey, string value, bool httpOnly = true, bool secure = true, SameSiteMode sameSite = SameSiteMode.Strict, DateTimeOffset? expiration = null);
        void DeleteCookie(HttpResponse response, string cookieKey, bool httpOnly = true, bool secure = true, SameSiteMode sameSite = SameSiteMode.Strict);
        string GetCookie(HttpRequest request, string cookieKey);
    }
}

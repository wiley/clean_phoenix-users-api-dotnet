using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using WLSUser.Services.Authentication;
using WLSUser.Domain.Models.Authentication;

namespace WLSUser.Tests.Services
{
    public class CookiesServiceTests
    {
        private readonly ILogger<CookiesService> _logger;
        private readonly CookiesService _cookiesService;

        public CookiesServiceTests()
        {
            _logger = Substitute.For<ILogger<CookiesService>>();

            var options = new CookiesOptions
            {
                Cookies = new List<CookiesOptions.Cookie>
                {
                    new CookiesOptions.Cookie
                    {
                        Name = "SampleCookie",
                        Key = "__Secure-sample",
                        HttpOnly = false,
                        Secure = false,
                        SameSite = SameSiteMode.Lax,
                        Seconds = 10
                    }
                }
            };

            _cookiesService = new CookiesService(Options.Create<CookiesOptions>(options), _logger);
        }

        #region SetCookie

        [Fact]
        public void SetCookie_CustomKey_Secure_HttpOnly_SameSiteStrict_Success()
        {
            HttpContext httpContext = new DefaultHttpContext();

            string cookieKey = "testKey";
            string cookieValue = "cookieValue";

            _cookiesService.SetCookie(httpContext.Response, cookieKey, cookieValue);

            string setCookiesHeaderContent = httpContext.Response.Headers.FirstOrDefault(h => h.Key == "Set-Cookie").Value[0];

            setCookiesHeaderContent.Should().Contain($"{cookieKey}={cookieValue}");
            setCookiesHeaderContent.Should().Contain("secure");
            setCookiesHeaderContent.Should().Contain("samesite=strict");
            setCookiesHeaderContent.Should().Contain("httponly");
        }

        [Fact]
        public void SetCookie_CustomKey_NotSecure_NotHttpOnly_SameSiteNone_Success()
        {
            HttpContext httpContext = new DefaultHttpContext();

            string cookieKey = "testKey";
            string cookieValue = "cookieValue";

            _cookiesService.SetCookie(httpContext.Response, cookieKey, cookieValue, false, false, SameSiteMode.None);

            string setCookiesHeaderContent = httpContext.Response.Headers.FirstOrDefault(h => h.Key == "Set-Cookie").Value[0];

            setCookiesHeaderContent.Should().Contain($"{cookieKey}={cookieValue}");
            setCookiesHeaderContent.Should().NotContain("secure");
            setCookiesHeaderContent.Should().Contain("samesite=none");
            setCookiesHeaderContent.Should().NotContain("httponly");
        }

        [Fact]
        public void SetCookie_CustomKey_SetDateTimeOffset_Success()
        {
            HttpContext httpContext = new DefaultHttpContext();

            string cookieKey = "testKey";
            string cookieValue = "cookieValue";

            _cookiesService.SetCookie(httpContext.Response, cookieKey, cookieValue, false, false, SameSiteMode.None, DateTimeOffset.MinValue.AddSeconds(1337));

            string setCookiesHeaderContent = httpContext.Response.Headers.FirstOrDefault(h => h.Key == "Set-Cookie").Value[0];

            setCookiesHeaderContent.Should().Contain($"{cookieKey}={cookieValue}");
            setCookiesHeaderContent.Should().Contain("expires=Mon, 01 Jan 0001 00:22:17 GMT");
        }

        [Fact]
        public void SetCookie_ParsedKey_Success()
        {
            HttpContext httpContext = new DefaultHttpContext();

            string cookieKey = "SampleCookie";
            string parsedKey = "__Secure-sample";
            string cookieValue = "cookieValue";

            _cookiesService.SetCookie(httpContext.Response, cookieKey, cookieValue);

            string setCookiesHeaderContent = httpContext.Response.Headers.FirstOrDefault(h => h.Key == "Set-Cookie").Value[0];

            setCookiesHeaderContent.Should().Contain($"{parsedKey}={cookieValue}");
            setCookiesHeaderContent.Should().NotContain("secure");
            setCookiesHeaderContent.Should().Contain("samesite=lax");
            setCookiesHeaderContent.Should().NotContain("httponly");
        }

        #endregion

        #region DeleteCookie
        [Fact]
        public void DeleteCookie_CustomKey_Secure_HttpOnly_SameSiteStrict_Success()
        {
            HttpContext httpContext = new DefaultHttpContext();

            string cookieKey = "testKey";

            _cookiesService.DeleteCookie(httpContext.Response, cookieKey);

            string setCookiesHeaderContent = httpContext.Response.Headers.FirstOrDefault(h => h.Key == "Set-Cookie").Value[0];

            setCookiesHeaderContent.Should().Contain($"{cookieKey}=;");
            setCookiesHeaderContent.Should().Contain("secure");
            setCookiesHeaderContent.Should().Contain("samesite=strict");
            setCookiesHeaderContent.Should().Contain("httponly");
        }

        [Fact]
        public void DeleteCookie_CustomKey_NotSecure_HttpOnly_SameSiteNone_Success()
        {
            HttpContext httpContext = new DefaultHttpContext();

            string cookieKey = "testKey";

            _cookiesService.DeleteCookie(httpContext.Response, cookieKey, true, false, SameSiteMode.None);

            string setCookiesHeaderContent = httpContext.Response.Headers.FirstOrDefault(h => h.Key == "Set-Cookie").Value[0];

            setCookiesHeaderContent.Should().Contain($"{cookieKey}=;");
            setCookiesHeaderContent.Should().NotContain("secure");
            setCookiesHeaderContent.Should().Contain("samesite=none");
            setCookiesHeaderContent.Should().Contain("httponly");
        }

        [Fact]
        public void DeleteCookie_CustomKey_NotSecure_NotHttpOnly_SameSiteStrict_Success()
        {
            HttpContext httpContext = new DefaultHttpContext();

            string cookieKey = "testKey";

            _cookiesService.DeleteCookie(httpContext.Response, cookieKey, false, false);

            string setCookiesHeaderContent = httpContext.Response.Headers.FirstOrDefault(h => h.Key == "Set-Cookie").Value[0];

            setCookiesHeaderContent.Should().Contain($"{cookieKey}=;");
            setCookiesHeaderContent.Should().NotContain("secure");
            setCookiesHeaderContent.Should().Contain("samesite=strict");
            setCookiesHeaderContent.Should().NotContain("httponly");
        }

        [Fact]
        public void DeleteCookie_ParsedKey_Success()
        {
            HttpContext httpContext = new DefaultHttpContext();

            string cookieKey = "SampleCookie";
            string parsedKey = "__Secure-sample";

            _cookiesService.DeleteCookie(httpContext.Response, cookieKey);

            string setCookiesHeaderContent = httpContext.Response.Headers.FirstOrDefault(h => h.Key == "Set-Cookie").Value[0];

            setCookiesHeaderContent.Should().Contain($"{parsedKey}=;");
            setCookiesHeaderContent.Should().NotContain("secure");
            setCookiesHeaderContent.Should().Contain("samesite=lax");
            setCookiesHeaderContent.Should().NotContain("httponly");
        }
        #endregion

        #region GetCookie
        [Fact]
        public void GetCookie_CustomKey_Success()
        {
            HttpContext mockHttpContext = Substitute.For<HttpContext>();
            IRequestCookieCollection cookieCollection = Substitute.For<IRequestCookieCollection>();

            string cookieKey = "testKey";
            string cookieValue = "testValue";

            var cookieList = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>(cookieKey, cookieValue)
            };

            cookieCollection.GetEnumerator().Returns(e => cookieList.GetEnumerator());

            mockHttpContext.Request.Cookies.Returns(cookieCollection);

            string returnedCookieValue = _cookiesService.GetCookie(mockHttpContext.Request, cookieKey);

            Assert.Equal(cookieValue, returnedCookieValue);
        }

        [Fact]
        public void GetCookie_ParsedKey_Success()
        {
            HttpContext mockHttpContext = Substitute.For<HttpContext>();
            IRequestCookieCollection cookieCollection = Substitute.For<IRequestCookieCollection>();

            string cookieKey = "SampleCookie";
            string parsedKey = "__Secure-sample";
            string cookieValue = "testValue";

            var cookieList = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>(parsedKey, cookieValue)
            };

            cookieCollection.GetEnumerator().Returns(e => cookieList.GetEnumerator());

            mockHttpContext.Request.Cookies.Returns(cookieCollection);

            string returnedCookieValue = _cookiesService.GetCookie(mockHttpContext.Request, cookieKey);

            Assert.Equal(cookieValue, returnedCookieValue);
        }
        #endregion
    }
}

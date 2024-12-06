using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace WLSUser.Domain.Models.Authentication
{
    public class CookiesOptions
    {
        public class Cookie
        {
            public string Name { get; set; }
            public string Key { get; set; }
            public bool HttpOnly { get; set; }
            public bool Secure { get; set; }
            public SameSiteMode SameSite { get; set; }
            public int Hours { get; set; } = 0;
            public int Minutes { get; set; } = 0;
            public int Seconds { get; set; } = 0;
            public int TotalSeconds
            {
                get { return Hours * 3600 + Minutes * 60 + Seconds; }
            }
        }

        public List<Cookie> Cookies { get; set; }
    }
}
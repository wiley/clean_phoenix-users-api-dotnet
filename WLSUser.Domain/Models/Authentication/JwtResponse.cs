using System.Collections.Generic;

namespace WLSUser.Domain.Models.Authentication
{
    public class JwtResponse
    {
        public IDictionary<string, string> jwt { get; set; }
        public string redirect_url { get; set; }
    }
}

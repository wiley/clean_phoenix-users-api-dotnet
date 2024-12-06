using System.Collections.Generic;

namespace WLSUser.Domain.Models.Authentication
{
    public class JwtExpirations
    {
        public class Token
        {
            public string Type { get; set; }
            public int Hours { get; set; } = 0;
            public int Minutes { get; set; } = 0;
            public int Seconds { get; set; } = 0;
            public int TotalSeconds
            {
                get { return Hours * 3600 + Minutes * 60 + Seconds; }
            }
        }

        public List<Token> Tokens { get; set; }
    }
}

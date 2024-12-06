using System.Collections.Generic;

namespace WLSUser.Domain.Models
{
    public class RedisServiceOptions
    {
        public class CacheSetting
        {
            public string Key { get; set; }
            public int Minutes { get; set; } = 0;
            public int Seconds { get; set; } = 0;
            public int TotalSeconds
            {
                get { return Minutes * 60 + Seconds; }
            }
        }

        public string Connection { get; set; }
        public bool UnitTest { get; set; }
        public List<CacheSetting> CacheSettings { get; set; }
    }
}
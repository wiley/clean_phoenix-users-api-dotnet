using WLSUser.Domain.Models.Interfaces;

namespace WLSUser.Domain.Models
{
    public class AppConfig : IAppConfig
    {
        public string ConnectionString { get; set; }
        public string Environment { get; set; }
        public string PrivateKey { get; set; }
    }
}

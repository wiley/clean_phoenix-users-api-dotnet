namespace WLSUser.Domain.Models.Interfaces
{
    public interface IAppConfig
    {
        string ConnectionString { get; set; }
        string Environment { get; set; }
        string PrivateKey { get; set; }
    }
}

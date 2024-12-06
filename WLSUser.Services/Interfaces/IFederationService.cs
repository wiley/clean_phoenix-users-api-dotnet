using System.Threading.Tasks;
using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;

namespace WLSUser.Services.Interfaces
{
    public interface IFederationService
    {
        Task<Federation> GetFederationByName(string name, int siteId);
        Task<Federation> GetFederationByEmail(string email, int siteId);
        SsoFederationUrlResponse GetFederationUrl(Federation federation, string state);
        Task<string> StoreStateInformation(string stateInformation);
        Task<string> GetStateInformation(string stateKey);
    }
}

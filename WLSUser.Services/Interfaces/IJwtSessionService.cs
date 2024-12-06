using System.Collections.Generic;
using System.Threading.Tasks;

namespace WLSUser.Services.Interfaces
{
    public interface IJwtSessionService
    {
        string GetAuthUrl(string state);
        void SetAuthUrl(string state, string value);
        string GetFederationName(string state);
        void SetFederationName(string state, string value);
        string GetRedirectUrl(string state);
        void SetRedirectUrl(string state, string value);
        Task<Dictionary<string, string>> CreateJwtToken(string code, string state);
    }
}

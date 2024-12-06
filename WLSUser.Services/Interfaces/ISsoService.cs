using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WLSUser.Domain.Models;

namespace WLSUser.Services.Interfaces
{
    public interface ISsoService
    {
        FormUrlEncodedContent CreateSsoContent(Federation federation, string code, Dictionary<string, string> clientSecretDictionary);
        Dictionary<string, string> GetClientSecretDictionary(Federation federation);
        string GetClientSecretBasicAuthentication(Federation federation);
        Task<OpenIdToken> GetOpenIdToken(Federation federation, FormUrlEncodedContent content, string clientSecretBasicAuthentication);
    }
}

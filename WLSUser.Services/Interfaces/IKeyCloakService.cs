using System.Collections.Generic;
using System.Threading.Tasks;

using WLSUser.Domain.Models;
using WLSUser.Domain.Models.Authentication;

namespace WLSUser.Services.Interfaces
{
    public interface IKeyCloakService
    {
        Task<Dictionary<string, string>> GetJwt(string username);
        Task<bool> UserExists(string username);
        Task<bool> CreateUser(string username);
        Task DeleteUser(string keycloakUserId);
        Task<AuthResponseV4> GetPasswordTokens(UserModel user);
        Task<AuthResponseV4> GetApiTokens();
        Task Logout(string keycloakUserId);
    }
}

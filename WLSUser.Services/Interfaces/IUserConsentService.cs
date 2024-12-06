using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WLSUser.Domain.Models.V4;

namespace WLSUser.Services.Interfaces
{
    public interface IUserConsentService
    {
        Task<UserConsent> CreateUserConsent(int userId, CreateUserConsentRequest request);
        Task<IEnumerable<UserConsent>> SearchUserConsents(int userId, string policyType, bool latestVersion);
        Task<UserConsent> GetUserConsentById(int userId, int userConsentId);
        Task DeleteUserConsent(int userId, int userConsentId);
    }
}

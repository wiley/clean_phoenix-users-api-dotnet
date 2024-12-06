using System.Threading.Tasks;
using WLSUser.Domain.Models;

namespace WLSUser.Services.Interfaces
{
    public interface IEmailAPIService
    {
        Task<bool> RequestRecoverPassword(UserModel user, string code, SiteTypeEnum siteType);
    }
}

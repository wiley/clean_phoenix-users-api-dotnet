using System.Threading.Tasks;

namespace WLSUser.Services.Interfaces
{
    public interface ILearnerEmailAPI
    {
        Task<bool> RequestForgotPassword(string apiToken, string emailAddress);

        Task<bool> RequestHealthCheck();
    }
}
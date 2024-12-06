using System.Collections.Generic;
using System.Threading.Tasks;

namespace WLSUser.Services.Interfaces
{
    public interface IHealthService
    {
        bool PerformHealthCheck();
        Task<Dictionary<string, string>> VerifyDependencies();
        bool CheckDependenciesResult(Dictionary<string, string> results);
    }
}
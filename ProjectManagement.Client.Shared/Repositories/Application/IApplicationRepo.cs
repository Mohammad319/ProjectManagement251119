using ProjectManagement.Client.Shared.Model.Application;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Application
{
    public interface IApplicationRepo
    {
        Task<List<ApplicationModel>> GetApplicationsAsync(bool withNoneVisible);
        Task<int> CreateAsync(ApplicationValuesModel create);
        Task<bool> UpdateAsync(ApplicationValuesModel create);
    }
}

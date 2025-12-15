using Domain.Entities.Application;
using ProjectManagement.Shared.Base.Application;

namespace Application.Feature.Application
{
    public interface IApplicationService
    {
        Task<IEnumerable<ApplicationValuesEntity>> GetCalcAppAsync(int CalcId, CancellationToken cancellationToken);
        Task<List<ApplicationEntity>> GetApplicationQueryAsync(bool WithNoneVisible, CancellationToken cancellationToken);
        Task<int> CreateAsync(ApplicationEntity Dto, CancellationToken cancellationToken);
        Task<int> CreateCalcApp(ApplicationValuesBase Dto, int CalculationId, int ApplicationId, CancellationToken cancellationToken);
        Task<bool> DeleteApplecationAsync(int Id, CancellationToken cancellationToken);
        Task<bool> DeleteCalcAppAsync(int Id, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(ApplicationEntity dto, CancellationToken cancellationToken);
        Task<bool> UpdateCalcAppAsync(ApplicationValuesEntity dto, CancellationToken cancellationToken);
    }
}

using Domain.Entities.Application;
using ProjectManagement.Shared.Base.Application;

namespace Application.Feature.Application
{
    public interface IApplicationService
    {
        Task<IEnumerable<ApplicationValuesEntity>> GetCalcAppAsync(int calcId, CancellationToken cancellationToken);
        Task<List<ApplicationEntity>> GetApplicationQueryAsync(bool withNoneVisible, CancellationToken cancellationToken);

        Task<IReadOnlyList<ApplicationListItemDto>> GetApplicationListAsync(bool withNoneVisible, CancellationToken cancellationToken);
        Task<IReadOnlyList<ApplicationValueListItemDto>> GetCalcAppListAsync(int calcId, CancellationToken cancellationToken);

        Task<int> CreateAsync(ApplicationEntity dto, CancellationToken cancellationToken);
        Task<int> CreateCalcApp(ApplicationValuesBase dto, int calculationId, int applicationId, CancellationToken cancellationToken);

        Task<bool> DeleteApplicationAsync(int id, CancellationToken cancellationToken);
        Task<bool> DeleteApplecationAsync(int id, CancellationToken cancellationToken);
        Task<bool> DeleteCalcAppAsync(int id, CancellationToken cancellationToken);

        Task<bool> UpdateAsync(ApplicationEntity dto, CancellationToken cancellationToken);
        Task<bool> UpdateCalcAppAsync(ApplicationValuesEntity dto, CancellationToken cancellationToken);
    }
}

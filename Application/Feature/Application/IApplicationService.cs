using ProjectManagement.Shared.DTO.App;

namespace Application.Feature.Application
{
    public interface IApplicationService
    {
        Task<List<ApplicationValuesDTO>> GetCalcAppAsync(int calcId, CancellationToken cancellationToken);
        Task<List<ApplicationDTO>> GetApplicationQueryAsync(bool withNoneVisible, CancellationToken cancellationToken);

        Task<IReadOnlyList<ApplicationListItemDto>> GetApplicationListAsync(bool withNoneVisible, CancellationToken cancellationToken);
        Task<IReadOnlyList<ApplicationValueListItemDto>> GetCalcAppListAsync(int calcId, CancellationToken cancellationToken);

        Task<int> CreateAsync(ApplicationDTO dto, CancellationToken cancellationToken);
        Task<int> CreateCalcApp(ApplicationValuesDTO dto, CancellationToken cancellationToken);

        Task<bool> DeleteApplicationAsync(int id, CancellationToken cancellationToken);
        Task<bool> DeleteApplecationAsync(int id, CancellationToken cancellationToken);
        Task<bool> DeleteCalcAppAsync(int id, CancellationToken cancellationToken);

        Task<bool> UpdateAsync(ApplicationDTO dto, CancellationToken cancellationToken);
        Task<bool> UpdateCalcAppAsync(ApplicationValuesDTO dto, CancellationToken cancellationToken);
    }
}

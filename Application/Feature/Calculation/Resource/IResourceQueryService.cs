using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Resource
{
    public interface IResourceQueryService
    {
        Task<List<ResourceListDTO>> GetByFilterAsync(
            FilterCalculationItemsDto filter,
            CancellationToken cancellationToken = default);
    }
}

using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.General
{
    public interface ILookupStatusCommandService<TEntity>
    {
        /// <summary>
        /// يرجع كل الكيانات كما هي (للإدارة / شاشات الإعدادات).
        /// </summary>
        Task<List<TEntity>> GetAllAsync(CancellationToken ct = default);

        /// <summary>
        /// يرجع قائمة خفيفة (Id, Name, SortOrder) للـ dropdowns:
        /// - لو Id = null => يرجع فقط العناصر المرئية (IsVisible = true).
        /// - لو Id != null => يرجع كل العناصر المرئية + هذا العنصر حتى لو كان IsVisible = false.
        /// </summary>
        Task<IEnumerable<ListOrderDTO>> GetVisualAsync(int? id, CancellationToken ct = default);

        Task<int> CreateAsync(PostTaskStatusDTO dto, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, PostTaskStatusDTO dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}

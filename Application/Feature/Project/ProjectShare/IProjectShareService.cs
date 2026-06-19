using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.ProjectShare
{
    /// <summary>
    /// Intern delning på projektnivå (separat från kalkyl-/avdelningsdelningen i CalcShare).
    /// En delning = ett projekt delat med EN mottagare (användare eller avdelning) med en roll
    /// och en lista valda kalkyler.
    /// </summary>
    public interface IProjectShareService
    {
        Task<IReadOnlyList<ProjectShareListItemDTO>> GetByProjectAsync(Guid projectId, int? departmentId, int userId, CancellationToken ct = default);
        Task<int> UpsertAsync(Guid projectId, ProjectShareUpsertDTO dto, int userId, int? departmentId, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, int userId, int? departmentId, CancellationToken ct = default);
    }
}

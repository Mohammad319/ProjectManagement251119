using Application.Interfaces;
using ProjectManagement.Shared.DTO.UserSettings;

namespace Application.Feature.General.ListSettings.Queries
{
    public sealed record GetUserListSettingsQuery(int UserId, string Scope) : IRequest<List<UserListSettingDTO>>;

    public sealed class GetUserListSettingsQueryHandler(IUserListSettingService service)
        : IRequestHandler<GetUserListSettingsQuery, List<UserListSettingDTO>>
    {
        public Task<List<UserListSettingDTO>> Handle(GetUserListSettingsQuery request, CancellationToken ct)
            => service.GetByScopeAsync(request.UserId, request.Scope, ct);
    }
}

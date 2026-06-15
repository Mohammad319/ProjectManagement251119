using Application.Interfaces;

namespace Application.Feature.General.ListSettings.Commands
{
    public sealed record UpsertUserListSettingCommand(int UserId, string Scope, string Kind, string Payload) : IRequest<bool>;

    public sealed class UpsertUserListSettingCommandHandler(IUserListSettingService service)
        : IRequestHandler<UpsertUserListSettingCommand, bool>
    {
        public async Task<bool> Handle(UpsertUserListSettingCommand request, CancellationToken ct)
        {
            await service.UpsertAsync(request.UserId, request.Scope, request.Kind, request.Payload, ct);
            return true;
        }
    }
}

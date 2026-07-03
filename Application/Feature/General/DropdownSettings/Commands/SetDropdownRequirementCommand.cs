using Application.Interfaces;

namespace Application.Feature.General.DropdownSettings.Commands
{
    public sealed record SetDropdownRequirementCommand(string Category, bool IsRequired) : IRequest<bool>;

    public sealed class SetDropdownRequirementCommandHandler(IDropdownSettingService service)
        : IRequestHandler<SetDropdownRequirementCommand, bool>
    {
        public async Task<bool> Handle(SetDropdownRequirementCommand request, CancellationToken ct)
        {
            await service.SetRequiredAsync(request.Category, request.IsRequired, ct);
            return true;
        }
    }
}

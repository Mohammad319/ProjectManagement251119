using Application.Interfaces;
using Application.Services.CalculationItems.TemplateTable;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Application.Feature.Calculation.TemplateTable.Commands
{
    public sealed record CreateTemplateColumnCommand(TemplateColumnPostDTO Dto, int? DepartmentId)
        : IRequest<TemplateColumnModelDTO>;

    public sealed class CreateTemplateColumnCommandHandler(ITemplateColumnCommandService service)
        : IRequestHandler<CreateTemplateColumnCommand, TemplateColumnModelDTO>
    {
        public Task<TemplateColumnModelDTO> Handle(CreateTemplateColumnCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, request.DepartmentId, ct);
    }

    public sealed record UpdateTemplateColumnCommand(TemplateColumnPostDTO Dto, int Id, int? DepartmentId)
        : IRequest<bool>;

    public sealed class UpdateTemplateColumnCommandHandler(ITemplateColumnCommandService service)
        : IRequestHandler<UpdateTemplateColumnCommand, bool>
    {
        public Task<bool> Handle(UpdateTemplateColumnCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, request.DepartmentId, ct);
    }

    public sealed record DeleteTemplateColumnCommand(int Id, int? DepartmentId)
        : IRequest<bool>;

    public sealed class DeleteTemplateColumnCommandHandler(ITemplateColumnCommandService service)
        : IRequestHandler<DeleteTemplateColumnCommand, bool>
    {
        public Task<bool> Handle(DeleteTemplateColumnCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, request.DepartmentId, ct);
    }

    public sealed record CopyTemplateColumnCommand(int Id, int? SourceDepartmentId, int? TargetDepartmentId)
        : IRequest<TemplateColumnModelDTO?>;

    public sealed class CopyTemplateColumnCommandHandler(ITemplateColumnCommandService service)
        : IRequestHandler<CopyTemplateColumnCommand, TemplateColumnModelDTO?>
    {
        public Task<TemplateColumnModelDTO?> Handle(CopyTemplateColumnCommand request, CancellationToken ct)
            => service.CopyAsync(request.Id, request.SourceDepartmentId, request.TargetDepartmentId, ct);
    }

    public sealed record SetDefaultTemplateColumnCommand(int CalculationId, int? TemplateColumnId, int? DepartmentId)
        : IRequest<TemplateColumnModelDTO?>;

    public sealed class SetDefaultTemplateColumnCommandHandler(ITemplateColumnCommandService service)
        : IRequestHandler<SetDefaultTemplateColumnCommand, TemplateColumnModelDTO?>
    {
        public Task<TemplateColumnModelDTO?> Handle(SetDefaultTemplateColumnCommand request, CancellationToken ct)
            => service.SetDefaultAsync(request.CalculationId, request.TemplateColumnId, request.DepartmentId, ct);
    }
}

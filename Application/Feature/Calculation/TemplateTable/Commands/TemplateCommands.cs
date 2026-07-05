using Application.Interfaces;
using Application.Services.CalculationItems.TemplateTable;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Application.Feature.Calculation.TemplateTable.Commands
{
    // ----------------------------------------------------------
    // CREATE TEMPLATE
    // ----------------------------------------------------------
    public sealed record CreateTemplateCommand(TemplateListPostDTO Dto, int? DepartmentId) : IRequest<TemplateModelDTO>;

    public sealed class CreateTemplateCommandHandler(ITemplateCommandService service)
                : IRequestHandler<CreateTemplateCommand, TemplateModelDTO>
    {
        public Task<TemplateModelDTO> Handle(CreateTemplateCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, request.DepartmentId, ct);
    }

    // ----------------------------------------------------------
    // UPDATE TEMPLATE
    // ----------------------------------------------------------
    public sealed record UpdateTemplateCommand(TemplateListPostDTO Dto, int Id, int? DepartmentId)
        : IRequest<bool>;

    public sealed class UpdateTemplateCommandHandler
        : IRequestHandler<UpdateTemplateCommand, bool>
    {
        private readonly ITemplateCommandService _service;

        public UpdateTemplateCommandHandler(ITemplateCommandService service)
        {
            _service = service;
        }

        public Task<bool> Handle(UpdateTemplateCommand request, CancellationToken ct)
            => _service.UpdateAsync(request.Id, request.Dto, request.DepartmentId, ct);
    }

    // ----------------------------------------------------------
    // DELETE TEMPLATE
    // ----------------------------------------------------------
    public sealed record DeleteTemplateCommand(int Id, int? DepartmentId)
        : IRequest<bool>;

    public sealed class DeleteTemplateCommandHandler
        : IRequestHandler<DeleteTemplateCommand, bool>
    {
        private readonly ITemplateCommandService _service;

        public DeleteTemplateCommandHandler(ITemplateCommandService service)
        {
            _service = service;
        }

        public Task<bool> Handle(DeleteTemplateCommand request, CancellationToken ct)
            => _service.DeleteAsync(request.Id, request.DepartmentId, ct);
    }

    // ----------------------------------------------------------
    // COPY TEMPLATE (creates an editable "Kopia av …")
    // ----------------------------------------------------------
    public sealed record CopyTemplateCommand(int Id, int? SourceDepartmentId, int? TargetDepartmentId)
        : IRequest<TemplateModelDTO?>;

    public sealed class CopyTemplateCommandHandler(ITemplateCommandService service)
                : IRequestHandler<CopyTemplateCommand, TemplateModelDTO?>
    {
        public Task<TemplateModelDTO?> Handle(CopyTemplateCommand request, CancellationToken ct)
            => service.CopyAsync(request.Id, request.SourceDepartmentId, request.TargetDepartmentId, ct);
    }

    // ----------------------------------------------------------
    // SET DEFAULT TEMPLATE
    // ----------------------------------------------------------
    public sealed record SetDefaultTemplateCommand(int CalculationId, int? TemplateId, int? DepartmentId)
        : IRequest<TemplateModelDTO?>;

    public sealed class SetDefaultTemplateCommandHandler(ITemplateCommandService service)
                : IRequestHandler<SetDefaultTemplateCommand, TemplateModelDTO?>
    {
        public Task<TemplateModelDTO?> Handle(SetDefaultTemplateCommand request, CancellationToken ct)
            => service.SetDefaultAsync(request.CalculationId, request.TemplateId, request.DepartmentId, ct);
    }
}

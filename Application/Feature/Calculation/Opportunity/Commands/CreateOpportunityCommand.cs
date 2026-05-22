using Application.Interfaces;
using Application.Services.CalculationItems.Opportunity;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Opportunity.Commands
{
    // CREATE
    public sealed record CreateOpportunityCommand(PostOpportunityDTO Dto, int CalculationId, int? DepartmentId) : IRequest<int>;

    public sealed class CreateOpportunityCommandHandler
        : IRequestHandler<CreateOpportunityCommand, int>
    {
        private readonly IOpportunityService _service;

        public CreateOpportunityCommandHandler(IOpportunityService service)
        {
            _service = service;
        }

        public Task<int> Handle(CreateOpportunityCommand request, CancellationToken ct)
            => _service.CreateAsync(request.Dto, request.CalculationId, request.DepartmentId, ct);
    }

    // UPDATE
    public sealed record UpdateOpportunityCommand(PostOpportunityDTO Dto, int Id, int? DepartmentId) : IRequest<bool>;

    public sealed class UpdateOpportunityCommandHandler
        : IRequestHandler<UpdateOpportunityCommand, bool>
    {
        private readonly IOpportunityService _service;

        public UpdateOpportunityCommandHandler(IOpportunityService service)
        {
            _service = service;
        }

        public Task<bool> Handle(UpdateOpportunityCommand request, CancellationToken ct)
            => _service.UpdateAsync(request.Id, request.Dto, request.DepartmentId, ct);
    }

    // DELETE
    public sealed record DeleteOpportunityCommand(int Id, int? DepartmentId) : IRequest<bool>;

    public sealed class DeleteOpportunityCommandHandler
        : IRequestHandler<DeleteOpportunityCommand, bool>
    {
        private readonly IOpportunityService _service;

        public DeleteOpportunityCommandHandler(IOpportunityService service)
        {
            _service = service;
        }

        public Task<bool> Handle(DeleteOpportunityCommand request, CancellationToken ct)
            => _service.DeleteAsync(request.Id, request.DepartmentId, ct);
    }
}

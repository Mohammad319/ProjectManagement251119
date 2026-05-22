using Application.Interfaces;
using Application.Services.CalculationItems.Tender;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Tender.Commands
{
    // =========================================================
    // Tender Commands (Create / Update / Delete / UpdateAttrValue)
    // =========================================================

    // --------- CreateTender ---------
    public sealed record CreateTenderCommand(
        TenderPostDTO Dto,
        int CalculationId,
        int CompanyId,
        int? DepartmentId
    ) : IRequest<int>;

    public sealed class CreateTenderCommandHandler(ITenderCommandService service)
                : IRequestHandler<CreateTenderCommand, int>
    {
        public Task<int> Handle(CreateTenderCommand request, CancellationToken cancellationToken)
            => service.CreateTenderAsync(request.Dto, request.CalculationId, request.CompanyId, request.DepartmentId, cancellationToken);
    }

    // --------- UpdateTender ---------
    public sealed record UpdateTenderCommand(
        TenderPostDTO Dto,
        int Id,
        int CalculationId,
        int? DepartmentId
    ) : IRequest<bool>;

    public sealed class UpdateTenderCommandHandler(ITenderCommandService service)
                : IRequestHandler<UpdateTenderCommand, bool>
    {
        public Task<bool> Handle(UpdateTenderCommand request, CancellationToken cancellationToken)
            => service.UpdateTenderAsync(request.Id, request.CalculationId, request.Dto, request.DepartmentId, cancellationToken);
    }

    // --------- DeleteTender ---------
    public sealed record DeleteTenderCommand(
        int Id,
        int CalculationId,
        int? DepartmentId
    ) : IRequest<bool>;

    public sealed class DeleteTenderCommandHandler(ITenderCommandService service)
                : IRequestHandler<DeleteTenderCommand, bool>
    {
        public Task<bool> Handle(DeleteTenderCommand request, CancellationToken cancellationToken)
            => service.DeleteTenderAsync(request.Id, request.CalculationId, request.DepartmentId, cancellationToken);
    }

    // --------- UpdateTenderAttributeBind (قيمة متغير لعطاء) ---------
    public sealed record UpdateTenderAttributeBindCommand(
        int TenderId,
        int AttributeId,
        decimal AttrValue,
        int? DepartmentId
    ) : IRequest<bool>;

    public sealed class UpdateTenderAttributeBindCommandHandler(ITenderCommandService service)
                : IRequestHandler<UpdateTenderAttributeBindCommand, bool>
    {
        public Task<bool> Handle(UpdateTenderAttributeBindCommand request, CancellationToken cancellationToken)
            => service.UpdateTenderAttributeValueAsync(request.TenderId, request.AttributeId, request.AttrValue, request.DepartmentId, cancellationToken);
    }

    // =========================================================
    // TenderAttribute Commands (Create / Update / Delete)
    // =========================================================

    // --------- CreateTenderAttribute ---------
    public sealed record CreateTenderAttributeCommand(
        TenderAttributeListPostDTO Dto,
        int CalculationId,
        int? DepartmentId
    ) : IRequest<int>;

    public sealed class CreateTenderAttributeCommandHandler(ITenderAttributeCommandService service)
                : IRequestHandler<CreateTenderAttributeCommand, int>
    {
        public Task<int> Handle(CreateTenderAttributeCommand request, CancellationToken cancellationToken)
            => service.CreateAttributeAsync(request.Dto, request.CalculationId, request.DepartmentId, cancellationToken);
    }

    // --------- UpdateTenderAttribute ---------
    public sealed record UpdateTenderAttributeCommand(
        TenderAttributePostDTO Dto,
        int Id,
        int CalculationId,
        int? DepartmentId
    ) : IRequest<bool>;

    public sealed class UpdateTenderAttributeCommandHandler(ITenderAttributeCommandService service)
                : IRequestHandler<UpdateTenderAttributeCommand, bool>
    {
        public Task<bool> Handle(UpdateTenderAttributeCommand request, CancellationToken cancellationToken)
            => service.UpdateAttributeAsync(request.Id, request.CalculationId, request.Dto, request.DepartmentId, cancellationToken);
    }

    // --------- DeleteTenderAttribute ---------
    public sealed record DeleteTenderAttributeCommand(
        int Id,
        int CalculationId,
        int? DepartmentId
    ) : IRequest<bool>;

    public sealed class DeleteTenderAttributeCommandHandler(ITenderAttributeCommandService service)
                : IRequestHandler<DeleteTenderAttributeCommand, bool>
    {
        public Task<bool> Handle(DeleteTenderAttributeCommand request, CancellationToken cancellationToken)
            => service.DeleteAttributeAsync(request.Id, request.CalculationId, request.DepartmentId, cancellationToken);
    }
}

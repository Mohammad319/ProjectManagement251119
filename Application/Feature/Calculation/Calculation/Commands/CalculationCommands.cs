using Application.Interfaces;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Application.Feature.Calculation.Calculation.Commands
{
    public sealed record DeleteCalculationCommand(int Id, int UserId, int? DepartmentId) : IRequest<bool>;
    public class DeleteCalculationCommandHandler(ICalculationService service) : IRequestHandler<DeleteCalculationCommand, bool>
    {
        public Task<bool> Handle(DeleteCalculationCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, request.UserId, request.DepartmentId, ct);
    }

    public sealed record CopyCalculationCommand(int Id, Guid ProjectId, int? DepartmentId, int UserId, bool AllowCrossDepartment) : IRequest<int>;
    public class CopyCalculationCommandHandler(ICalculationService service) : IRequestHandler<CopyCalculationCommand, int>
    {
        public Task<int> Handle(CopyCalculationCommand request, CancellationToken ct)
            => service.CopyAsync(request.Id, request.ProjectId, request.DepartmentId, request.UserId, request.AllowCrossDepartment, ct);
    }

    public sealed record CreateProductionCalculationCommand(int Id, int? DepartmentId, int UserId) : IRequest<int>;
    public class CreateProductionCalculationCommandHandler(ICalculationService service) : IRequestHandler<CreateProductionCalculationCommand, int>
    {
        public Task<int> Handle(CreateProductionCalculationCommand request, CancellationToken ct)
            => service.CreateProductionCopyAsync(request.Id, request.DepartmentId, request.UserId, ct);
    }

    public sealed record CreateCalculationVersionCommand(int Id, int? DepartmentId, int UserId) : IRequest<int>;
    public class CreateCalculationVersionCommandHandler(ICalculationService service) : IRequestHandler<CreateCalculationVersionCommand, int>
    {
        public Task<int> Handle(CreateCalculationVersionCommand request, CancellationToken ct)
            => service.CreateVersionAsync(request.Id, request.DepartmentId, request.UserId, ct);
    }

    public sealed record MoveCalculationCommand(int Id, Guid ProjectId, int? DepartmentId, int UserId, bool AllowCrossDepartment) : IRequest<bool>;
    public class MoveCalculationCommandHandler(ICalculationService service) : IRequestHandler<MoveCalculationCommand, bool>
    {
        public Task<bool> Handle(MoveCalculationCommand request, CancellationToken ct)
            => service.MoveAsync(request.Id, request.ProjectId, request.DepartmentId, request.UserId, request.AllowCrossDepartment, ct);
    }

    public sealed record CreateCalculationCommand(CalculationPostDTO Dto, Guid ProjectId, int UserId, int? DepartmentId) : IRequest<int>;
    public class CreateCalculationCommandHandler(ICalculationService service) : IRequestHandler<CreateCalculationCommand, int>
    {
        public Task<int> Handle(CreateCalculationCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, request.ProjectId, request.UserId, request.DepartmentId, ct);
    }

    public sealed record HourlyPriceListCommand(int Id, List<HourlyPriceListGroupDTO> HourlyPriceList, int UserId, int? DepartmentId) : IRequest<bool>;
    public class HourlyPriceListCommandHandler(ICalculationService service) : IRequestHandler<HourlyPriceListCommand, bool>
    {
        public Task<bool> Handle(HourlyPriceListCommand request, CancellationToken ct)
            => service.UpdateHourlyPriceListAsync(request.Id, request.HourlyPriceList, request.UserId, request.DepartmentId, ct);
    }

    public sealed record NewOrderCalculationCommand(int Id, int NewOrder, int? DepartmentId) : IRequest<bool>;
    public class NewOrderCalculationCommandHandler(ICalculationService service) : IRequestHandler<NewOrderCalculationCommand, bool>
    {
        public Task<bool> Handle(NewOrderCalculationCommand request, CancellationToken ct)
            => service.NewOrderAsync(request.Id, request.NewOrder, request.DepartmentId, ct);
    }

    public sealed record UpdateCalculationCommand(CalculationPostDTO dto, int Id, int UserId, int? DepartmentId) : IRequest<bool>;

    public class UpdateCalculationCommandHandler(ICalculationService service) : IRequestHandler<UpdateCalculationCommand, bool>
    {
        public Task<bool> Handle(UpdateCalculationCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.dto, request.UserId, request.DepartmentId, ct);
    }

    public sealed record UpdateFactorsCommand(List<OHFactors> model, int Id, int? DepartmentId) : IRequest<bool>;
    public class UpdateFactorsCommandHandler(ICalculationService service) : IRequestHandler<UpdateFactorsCommand, bool>
    {
        public Task<bool> Handle(UpdateFactorsCommand request, CancellationToken ct)
            => service.UpdateFactorsAsync(request.Id, request.model, request.DepartmentId, ct);
    }

    public sealed record UpdateQuantityListCommand(List<QuanityListDTO> model, int Id, int? DepartmentId) : IRequest<bool>;
    public class UpdateQuantityListCommandHandler(ICalculationService service) : IRequestHandler<UpdateQuantityListCommand, bool>
    {
        public Task<bool> Handle(UpdateQuantityListCommand request, CancellationToken ct)
            => service.UpdateQuantityListAsync(request.Id, request.model, request.DepartmentId, ct);
    }

    public sealed record UpdateCalculationSortCommand(SortConfig Sort, int Id, int UserId, int? DepartmentId) : IRequest<bool>;
    public class UpdateCalculationSortCommandHandler(ICalculationService service) : IRequestHandler<UpdateCalculationSortCommand, bool>
    {
        public Task<bool> Handle(UpdateCalculationSortCommand request, CancellationToken ct)
            => service.UpdateSortAsync(request.Id, request.Sort, request.UserId, request.DepartmentId, ct);
    }

    public sealed record UpdateDisplayPresetsCommand(DisplayOptionsPresetStore Store, int Id, int? DepartmentId) : IRequest<bool>;
    public class UpdateDisplayPresetsCommandHandler(ICalculationService service) : IRequestHandler<UpdateDisplayPresetsCommand, bool>
    {
        public Task<bool> Handle(UpdateDisplayPresetsCommand request, CancellationToken ct)
            => service.UpdateDisplayPresetsAsync(request.Id, request.Store, request.DepartmentId, ct);
    }
}

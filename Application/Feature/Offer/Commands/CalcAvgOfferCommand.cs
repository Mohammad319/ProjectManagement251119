using Application.Feature.Offer;
using Application.Interfaces;
using ProjectManagement.Shared.DTO.Offer;

namespace Application.Feature.Calculation.Offer.Commands
{
    // -------------------------
    // CREATE
    // -------------------------
    public sealed record CreateOfferCommand(PostOfferDTO Dto, int? DepartmentId) : IRequest<int>;

    public sealed class CreateOfferCommandHandler(IOfferService service)
        : IRequestHandler<CreateOfferCommand, int>
    {
        public Task<int> Handle(CreateOfferCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, request.DepartmentId, ct);
    }

    // -------------------------
    // UPDATE
    // -------------------------
    public sealed record UpdateOfferCommand(int Id, PostOfferDTO Dto, int? DepartmentId) : IRequest<bool>;

    public sealed class UpdateOfferCommandHandler(IOfferService service)
        : IRequestHandler<UpdateOfferCommand, bool>
    {
        public Task<bool> Handle(UpdateOfferCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, request.DepartmentId, ct);
    }

    // -------------------------
    // DELETE
    // -------------------------
    public sealed record DeleteOfferCommand(int Id, int? DepartmentId) : IRequest<bool>;

    public sealed class DeleteOfferCommandHandler(IOfferService service)
        : IRequestHandler<DeleteOfferCommand, bool>
    {
        public Task<bool> Handle(DeleteOfferCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, request.DepartmentId, ct);
    }

    // -------------------------
    // SET OFFER (Primary offer on resource)
    // file: SetOfferQuery.cs (سمّيته Query عندك لكن هو Command عملياً)
    // -------------------------
    public sealed record SetOfferCommand(int ResourceId, int? OfferId, int? DepartmentId) : IRequest<bool>;

    public sealed class SetOfferCommandHandler(IOfferService service)
        : IRequestHandler<SetOfferCommand, bool>
    {
        public Task<bool> Handle(SetOfferCommand request, CancellationToken ct)
            => service.SetPrimaryOfferAsync(request.ResourceId, request.OfferId, request.DepartmentId, ct);
    }

    // -------------------------
    // CALC AVG OFFER
    // -------------------------
    public sealed record CalcAvgOfferCommand(int CalculationId, int OrganisationId, double Avg, int? DepartmentId) : IRequest<bool>;

    public sealed class CalcAvgOfferCommandHandler(IOfferService service)
        : IRequestHandler<CalcAvgOfferCommand, bool>
    {
        public Task<bool> Handle(CalcAvgOfferCommand request, CancellationToken ct)
            => service.CalcAvgOfferAsync(request.CalculationId, request.OrganisationId, request.Avg, request.DepartmentId, ct);
    }
}

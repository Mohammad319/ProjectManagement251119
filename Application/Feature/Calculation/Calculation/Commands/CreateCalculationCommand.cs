using Application.Interfaces;
using AutoMapper;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System;

namespace Application.Feature.Calculation.Calculation.Commands
{
    public sealed record CreateCalculationCommand(CalculationPostDTO Dto, Guid ProjectId, int UserId, int? DepartmentId) : IRequest<int>;
    public class CreateCalculationCommandHandler(ICalculationService service) : IRequestHandler<CreateCalculationCommand, int>
    {
        public Task<int> Handle(CreateCalculationCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, request.ProjectId, request.UserId, ct);
    }
}

using Application.Interfaces;
using Domain.Entities.Calculation;
using System;

namespace Application.Feature.Calculation.Calculation.Commands
{
    public sealed record CopyCalculationCommand(int Id, Guid ProjectId, int? DepartmentId, int UserId) : IRequest<int>;

    public class CopyCalculationCommandHandler(IShardingSingleDbContext _context) : IRequestHandler<CopyCalculationCommand, int>
    {
        public async Task<int> Handle(CopyCalculationCommand request, CancellationToken cancellationToken)
        {
            var calc = await _context.Calculation.FirstOrDefaultAsync(x => x.Id == request.Id &&
            (x.Project.Folder.DepartmentId == request.DepartmentId) || request.DepartmentId == null, cancellationToken: cancellationToken);
            if (calc == null) return 0;

            CalculationEntity newCalc = new()
            {
                Name = calc.Name,
                Code = calc.Code,
                CompensationId = calc.CompensationId,
                ContractId = calc.ContractId,
                Created = DateTime.Now,
                OrganisationId = calc.OrganisationId,
                PublicationDate = calc.PublicationDate,
                DecisionDate = calc.DecisionDate,
                HourlyPriceFactorData = calc.HourlyPriceFactorData,

                EndDate = calc.EndDate,

                TemplateId = calc.TemplateId,
                Tax = calc.Tax,
                TypeId = calc.TypeId,
                TenderDeadline = calc.TenderDeadline,
                TenderQA = calc.TenderQA,
                LastModified = DateTime.Now,
                UserId = request.UserId,
                StartDate = calc.StartDate,
                StatusId = calc.StatusId,
                ProcurementMethodsId = calc.ProcurementMethodsId,
                Procurement = calc.Procurement,
                Tasks = calc.Tasks,
                ProjectId = request.ProjectId,
                Data = calc.Data,
            };
            double? max = _context.Calculation.Where(x => x.ProjectId == request.ProjectId).Max(x => (double?)x.Order);
            if (max.HasValue) newCalc.Order = max.Value + 100;
            else newCalc.Order = 100;

            foreach (var task in calc.Tasks)
            {
                foreach (var res in task.Resources)
                {
                    res.Id = 0;
                    res.TaskId = 0;
                    res.OpportunityId = null;
                    res.OfferId = 0;
                    //res.OfferResource = null;
                    //res.Offers = null;
                }
                task.OpportunityId = null;
                task.Id = 0;
                task.CalculationId = 0;
            }
            newCalc.UserId = request.UserId;
            newCalc.Created = DateTime.Now;
            _context.Calculation.Add(newCalc);
            await _context.SaveChangesAsync(cancellationToken);
            return newCalc.Id;
        }
    }
}

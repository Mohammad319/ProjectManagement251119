using Application.Feature.Calculation.Calculation.Commands;
using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System;

namespace Application.Services.CalculationItems.Calculation
{
    public interface ICalculationService
    {
        Task<CalculationPageDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<List<CalculationPageDTO>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken);

        Task<int> CopyAsync(int Id, Guid ProjectId, int? DepartmentId, int UserId, CancellationToken cancellationToken);
        Task<Guid> CreateAsync(CreateCalculationCommand command, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(UpdateCalculationCommand command, CancellationToken cancellationToken);
    }
    public class CalculationService(IShardingSingleDbContext _context, INotificationHub _notification) : ICalculationService
    {
        public async Task<int> CopyAsync(int Id, Guid ProjectId, int? DepartmentId, int UserId, CancellationToken cancellationToken)
        {
            var calc = await _context.Calculation.FirstOrDefaultAsync(x => x.Id == Id &&
            (x.Project.Folder.DepartmentId == DepartmentId) || DepartmentId == null, cancellationToken: cancellationToken);
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
                UserId = UserId,
                StartDate = calc.StartDate,
                StatusId = calc.StatusId,
                ProcurementMethodsId = calc.ProcurementMethodsId,
                Procurement = calc.Procurement,
                Tasks = calc.Tasks,
                ProjectId = ProjectId,
                Metadata = calc.Metadata,
            };
            double? max = _context.Calculation.Where(x => x.ProjectId == ProjectId).Max(x => (double?)x.SortOrder);
            if (max.HasValue) newCalc.SortOrder = max.Value + 100;
            else newCalc.SortOrder = 100;
            foreach (var task in calc.Tasks)
            {
                foreach (var res in task.Resources)
                {
                    res.Id = 0;
                    res.TaskId = 0;
                    res.OpportunityId = null;
                    res.OfferId = 0;
                }
                task.OpportunityId = null;
                task.Id = 0;
                task.CalculationId = 0;
            }
            newCalc.UserId = UserId;
            newCalc.Created = DateTime.Now;
            _context.Calculation.Add(newCalc);
            await _context.SaveChangesAsync(cancellationToken);
            return newCalc.Id;
        }

        public Task<Guid> CreateAsync(CreateCalculationCommand command, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<CalculationPageDTO>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<CalculationPageDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAsync(UpdateCalculationCommand command, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

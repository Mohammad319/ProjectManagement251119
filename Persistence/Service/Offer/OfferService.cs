using Application.Feature.Offer;
using Application.Interfaces;
using Domain.Entities.Calculation;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Hub;
using ProjectManagement.Shared.DTO.Offer;

namespace Persistence.Service.Offer
{
    public sealed class OfferService(IDbContextFactoryTenant dbFactory, INotificationHub hub) : IOfferService
    {

        // ---------------- Commands ----------------

        public async Task<int> CreateAsync(PostOfferDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var entity = new OfferEntity(
                resourceId: dto.ResourceId,
                organisationId: dto.OrganisationId,
                metadata: new OfferData
                {
                    Cost = dto.Cost,
                    BaseCost = dto.BaseCost,
                    Contact = dto.Contact
                },
                comment: dto.Comment
            );

            context.Offers.Add(entity);
            await context.SaveChangesAsync(ct);

            var offer = await context.Offers.Where(x => x.Id == entity.Id).Select(x => new
            {
                CalcID = x.Resource.Task.CalculationId,
                Offer = new ListOfferDTO()
                {
                    Id = x.Id,
                    BaseCost = x.Metadata.BaseCost,
                    Cost = x.Metadata.Cost,
                    Organisation = x.Organisation.Name,
                    Comment = x.Comment,
                    Date = x.Date,
                    OrganisationId = x.OrganisationId,
                    SubCategory = x.Organisation.OrganisationCategory.Name,
                    Category = x.Organisation.OrganisationCategory.ParentCategory.Name,

                    //UCDepartment = x.ContactOrganisation.Department,
                    //UCMobile = x.ContactOrganisation.Mobile,
                    //UCStatus = x.ContactOrganisation.Status.ToString(),
                    //UCTelefone = x.ContactOrganisation.Telefone,
                    //ContactId = x.ContactOrganisation.Id,
                    //UCLastName = x.ContactOrganisation.LastName,
                    //UCFirstName = x.ContactOrganisation.FirstName,
                }
            }).FirstOrDefaultAsync(cancellationToken: ct);
            await hub.SendNotificationAsync(offer.CalcID.ToString(), ObjectTypHub.Offer,
                OperationType.Add,
    new HubDataDto { ParentId = dto.ResourceId, Data = offer.Offer }
);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, PostOfferDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var offer = await context.Offers.FindAsync(id);
            if (offer == null) return false;

            offer.Update(
                organisationId: dto.OrganisationId,
                metadata: new OfferData
                {
                    Cost = dto.Cost,
                    BaseCost = dto.BaseCost,
                    Contact = dto.Contact
                },
                comment: dto.Comment
            );

            await context.SaveChangesAsync(ct);
            var result = await context.Offers.Where(x => x.Id == offer.Id).Select(x => new
            {
                CalcID = x.Resource.Task.CalculationId,
                Offer = new ListOfferDTO()
                {
                    Id = x.Id,
                    BaseCost = x.Metadata.BaseCost,
                    Cost = x.Metadata.Cost,
                    Organisation = x.Organisation.Name,
                    Comment = x.Comment,
                    Date = x.Date,
                    OrganisationId = x.OrganisationId,
                    SubCategory = x.Organisation.OrganisationCategory.Name,
                    Category = x.Organisation.OrganisationCategory.ParentCategory.Name
                }
            }).FirstOrDefaultAsync(cancellationToken: ct);
            List<ListOfferDTO> ll = [result.Offer];
            await hub.SendNotificationAsync(result.CalcID.ToString(), ObjectTypHub.Offer,
                OperationType.Update, new HubDataDto() { Data = ll, ParentId = 0 });
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var offer = await context.Offers
                .Include(x => x.Resource).ThenInclude(x => x.Task)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (offer == null) return false;
            var calculationId = offer.Resource.Task.CalculationId;
            if (offer.Resource.PrimaryOfferId == id)
                offer.Resource.PrimaryOfferId = null;

            context.Offers.Remove(offer);
            await context.SaveChangesAsync(ct);


            await hub.SendNotificationAsync(calculationId.ToString(), ObjectTypHub.Offer,
                OperationType.Remove, id);

            return true;
        }

        public async Task<bool> SetPrimaryOfferAsync(int resourceId, int? offerId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var resource = await context.Resources
                .Include(x => x.Offers)
                .FirstOrDefaultAsync(x => x.Id == resourceId, ct);

            if (resource == null) return false;
            var calculationId = await context.Tasks
                .Where(r => r.Id == resource.TaskId)
                .Select(r => r.CalculationId)
                .SingleAsync(ct);
            var offer = offerId.HasValue
                ? resource.Offers.FirstOrDefault(o => o.Id == offerId)
                : null;

            resource.PrimaryOfferId = offerId;

            if (offer != null)
            {
                resource.Metadata.Cost = offer.Metadata.Cost;
                resource.Metadata.BaseCost = offer.Metadata.BaseCost;
            }

            await context.SaveChangesAsync(ct);
            await hub.SendNotificationAsync(calculationId.ToString(), ObjectTypHub.Offer, OperationType.Update, new HubDataDto() { Parent = offerId.ToString(), ParentId = resourceId });

            return true;
        }

        public async Task<bool> CalcAvgOfferAsync(int calcId, int organisationId, double avg, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var offers = await context.Offers
                .Where(x => x.OrganisationId == organisationId &&
                            x.Resource.Task.CalculationId == calcId)
                .ToListAsync(ct);

            if (!offers.Any()) return false;

            double sum = offers.Sum(x => x.Metadata.Cost);

            foreach (var o in offers)
                o.SetBaseCost((o.Metadata.Cost * avg) / sum);

            await context.SaveChangesAsync(ct);
            List<ListOfferDTO> result = await context.Offers.Where(x => x.OrganisationId == organisationId &&
x.Resource.Task.CalculationId == calcId).Select(x => new ListOfferDTO()
{
    Id = x.Id,
    BaseCost = x.Metadata.BaseCost,
    Cost = x.Metadata.Cost,
    Organisation = x.Organisation.Name,
    Comment = x.Comment,
    Date = x.Date,
    OrganisationId = x.OrganisationId,
    SubCategory = x.Organisation.OrganisationCategory.Name,
    Category = x.Organisation.OrganisationCategory.ParentCategory.Name
}
).ToListAsync(cancellationToken: ct);

            var tt = new HubDataDto() { Data = result, ParentId = 0 };
            await hub.SendNotificationAsync(calcId.ToString(), ObjectTypHub.Offer, OperationType.Update, tt);

            return true;
        }

        // ---------------- Queries ----------------

        public async Task<List<ListOfferCalcInfo>> GetByFilterAsync(OfferFilterDTO f, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            IQueryable<OfferEntity> q = context.Offers.AsNoTracking();

            if (f.CalculationID > 0)
                q = q.Where(x => x.Resource.Task.CalculationId == f.CalculationID);
            else if (f.ProjectID.HasValue)
                q = q.Where(x => x.Resource.Task.Calculation.ProjectId == f.ProjectID);
            else if (f.FolderID.HasValue)
                q = q.Where(x => x.Resource.Task.Calculation.Project.FolderId == f.FolderID);

            if (f.ResType.HasValue)
                q = q.Where(x => x.Resource.ResType == f.ResType);
            if (f.ResourceTypeId.HasValue)
                q = q.Where(x => x.Resource.ResourceTypeId == f.ResourceTypeId);
            if (f.ResourceSortId.HasValue)
                q = q.Where(x => x.Resource.ResourceSortId == f.ResourceSortId);
            if (f.OrganisationId.HasValue)
                q = q.Where(x => x.OrganisationId == f.OrganisationId);

            return await q
                .OrderByDescending(x => x.Date)
                .Select(x => new ListOfferCalcInfo
                {
                    Id = x.Id,
                    BaseCost = x.Metadata.BaseCost,
                    Cost = x.Metadata.Cost,
                    Comment = x.Comment,
                    Date = x.Date,
                    Organisation = x.Organisation.Name,
                    Category = x.Organisation.OrganisationCategory.ParentCategory.Name,
                    SubCategory = x.Organisation.OrganisationCategory.Name,
                    ResName = x.Resource.Name,
                    TaskName = x.Resource.Task.Name,
                    TaskCode = x.Resource.Task.Metadata.Code,
                    CalcName = x.Resource.Task.Calculation.Name,
                    CalcCode = x.Resource.Task.Calculation.Code
                })
                .ToListAsync(ct);
        }
    }
}

using Application.Interfaces;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.DTO.Offer;
using ProjectManagement.Shared.DTO.Hub;
using Persistence.Context;
using Application.Feature.Offer;
using ProjectManagement.Shared.Enums;

namespace Persistence.Service.Offer
{
    public sealed class OfferService(ShardingSingleDbContext db, INotificationHub hub) : IOfferService
    {

        // ---------------- Commands ----------------

        public async Task<int> CreateAsync(PostOfferDTO dto, CancellationToken ct = default)
        {
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

            db.Offers.Add(entity);
            await db.SaveChangesAsync(ct);

            await NotifyAsync(entity.Resource.Task.CalculationId, OperationType.Add, entity.ResourceId);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, PostOfferDTO dto, CancellationToken ct = default)
        {
            var offer = await db.Offers.FindAsync(id);
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

            await db.SaveChangesAsync(ct);
            await NotifyAsync(offer.Resource.Task.CalculationId, OperationType.Update, 0);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var offer = await db.Offers
                .Include(x => x.Resource)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (offer == null) return false;

            if (offer.Resource.PrimaryOfferId == id)
                offer.Resource.PrimaryOfferId = null;

            db.Offers.Remove(offer);
            await db.SaveChangesAsync(ct);

            await NotifyAsync(offer.Resource.Task.CalculationId, OperationType.Remove, id);
            return true;
        }

        public async Task<bool> SetPrimaryOfferAsync(int resourceId, int? offerId, CancellationToken ct = default)
        {
            var resource = await db.Resources
                .Include(x => x.Offers)
                .FirstOrDefaultAsync(x => x.Id == resourceId, ct);

            if (resource == null) return false;

            var offer = offerId.HasValue
                ? resource.Offers.FirstOrDefault(o => o.Id == offerId)
                : null;

            resource.PrimaryOfferId = offerId;

            if (offer != null)
            {
                resource.Metadata.Cost = offer.Metadata.Cost;
                resource.Metadata.BaseCost = offer.Metadata.BaseCost;
            }

            await db.SaveChangesAsync(ct);
            await NotifyAsync(resource.Task.CalculationId, OperationType.Update, resourceId);
            return true;
        }

        public async Task<bool> CalcAvgOfferAsync(int calcId, int organisationId, double avg, CancellationToken ct = default)
        {
            var offers = await db.Offers
                .Where(x => x.OrganisationId == organisationId &&
                            x.Resource.Task.CalculationId == calcId)
                .ToListAsync(ct);

            if (!offers.Any()) return false;

            double sum = offers.Sum(x => x.Metadata.Cost);

            foreach (var o in offers)
                o.SetBaseCost((o.Metadata.Cost * avg) / sum);

            await db.SaveChangesAsync(ct);
            await NotifyAsync(calcId, OperationType.Update, 0);
            return true;
        }

        // ---------------- Queries ----------------

        public async Task<List<ListOfferCalcInfo>> GetByFilterAsync(OfferFilterDTO f, CancellationToken ct = default)
        {
            IQueryable<OfferEntity> q = db.Offers.AsNoTracking();

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

        private Task NotifyAsync(int calcId, OperationType type, int parentId)
        {
            return hub.SendNotificationAsync(
                calcId.ToString(),
                ObjectTypHub.Offer,
                type,
                new HubDataDto { ParentId = parentId }
            );
        }
    }
}

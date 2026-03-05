using Persistence.Serialization;
using System.Text.Json;
using Application.Feature.Offer;
using Application.Interfaces;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Hub;
using ProjectManagement.Shared.DTO.Offer;
using ProjectManagement.Shared.Exceptions;

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

            // استعلام واحد فقط لجلب البيانات اللازمة للإشعار (CalcId + DTO)
            var created = await context.Offers
                .AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(ProjectOfferWithCalcId())
                .FirstOrDefaultAsync(ct) ?? throw new InvalidOperationException("Offer was not found after creation.");
            await NotifyOfferAsync(
                calcId: created.CalcID,
                op: OperationType.Add,
                parentId: dto.ResourceId,
                data: created.Offer
            );

            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, PostOfferDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // ملاحظة مهمة:
            // Metadata مخزنة كـ JSON عبر ValueConverter (nvarchar(max)).
            // لذلك ExecuteUpdateAsync على x.Metadata.* لن يترجم لـ SQL بشكل موثوق.
            // هنا نفضّل Correctness على micro-optimization.
            var entity = await context.Offers
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity is null) return false;

            // optimistic concurrency: detect stale edits
            if (dto.RowVersion is { Length: > 0 })
                context.Entry(entity).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;

            entity.Update(
                organisationId: dto.OrganisationId,
                metadata: new OfferData
                {
                    Cost = dto.Cost,
                    BaseCost = dto.BaseCost,
                    Contact = dto.Contact
                },
                comment: dto.Comment
            );

            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyConflictException("Offer", id);
            }

            var updated = await context.Offers
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(ProjectOfferWithCalcId())
                .FirstOrDefaultAsync(ct);

            if (updated is null) return false;

            // نفس شكل إشعاراتك السابقة (List<ListOfferDTO>)
            List<ListOfferDTO> list = [updated.Offer];

            await NotifyOfferAsync(
                calcId: updated.CalcID,
                op: OperationType.Update,
                parentId: 0,
                data: list
            );

            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var offer = await context.Offers
                .Include(x => x.Resource).ThenInclude(x => x.Task)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (offer is null) return false;

            var calculationId = offer.Resource.Task.CalculationId;

            if (offer.Resource.PrimaryOfferId == id)
                offer.Resource.PrimaryOfferId = null;

            context.Offers.Remove(offer);
            await context.SaveChangesAsync(ct);

            await NotifyOfferAsync(
                calcId: calculationId,
                op: OperationType.Remove,
                parentId: 0,
                data: id
            );

            return true;
        }

        public async Task<bool> SetPrimaryOfferAsync(int resourceId, int? offerId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var resource = await context.Resources
                .Include(x => x.Offers)
                .FirstOrDefaultAsync(x => x.Id == resourceId, ct);

            if (resource is null) return false;

            var calculationId = await context.Tasks
                .Where(r => r.Id == resource.TaskId)
                .Select(r => r.CalculationId)
                .SingleAsync(ct);

            var offer = offerId.HasValue
                ? resource.Offers.FirstOrDefault(o => o.Id == offerId.Value)
                : null;

            resource.PrimaryOfferId = offerId;

            if (offer is not null)
            {
                resource.Metadata.Cost = offer.Metadata.Cost;
                resource.Metadata.BaseCost = offer.Metadata.BaseCost;
            
                resource.Metadata.Normalize();
}

            await context.SaveChangesAsync(ct);

            await hub.SendNotificationAsync(
                calculationId.ToString(),
                ObjectTypHub.Offer,
                OperationType.Update,
                new HubDataDto
                {
                    Parent = offerId?.ToString() ?? string.Empty, // Null-safe
                    ParentId = resourceId
                }
            );

            return true;
        }

        public async Task<bool> CalcAvgOfferAsync(int calcId, int organisationId, double avg, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var offers = await context.Offers
                .Where(x => x.OrganisationId == organisationId &&
                            x.Resource.Task.CalculationId == calcId)
                .ToListAsync(ct);

            if (offers.Count == 0) return false;

            decimal sum = offers.Sum(x => x.Metadata.Cost);
            if (sum == 0) return false;

            foreach (var o in offers)
                o.SetBaseCost((o.Metadata.Cost * (decimal)avg) / sum);

            await context.SaveChangesAsync(ct);

            var result = await context.Offers
                .AsNoTracking()
                .Where(x => x.OrganisationId == organisationId &&
                            x.Resource.Task.CalculationId == calcId)
                .Select(ProjectListOfferDto())
                .ToListAsync(ct);

            await NotifyOfferAsync(
                calcId: calcId,
                op: OperationType.Update,
                parentId: 0,
                data: result
            );

            return true;
        }

        // ---------------- Queries ----------------

        public async Task<List<ListOfferCalcInfo>> GetByFilterAsync(OfferFilterDTO f, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            IQueryable<OfferEntity> q = context.Offers.AsNoTracking();

            // ---------- Server-side (SQL) filters ----------
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

            // NOTE: OfferFilterDTO.Account refers to Resource.AccountId
            if (f.Account.HasValue)
                q = q.Where(x => x.Resource.AccountId == f.Account);

            // ---------- Money range filters (SQL via computed columns) ----------
            // Offers.Metadata is stored as JSON (nvarchar(max)). We expose Cost/BaseCost as persisted computed columns
            // (CostValue/BaseCostValue) so the database can filter efficiently.
            decimal? minCost = f.MinCost;
            decimal? maxCost = f.MaxCost;
            if (minCost.HasValue && maxCost.HasValue && minCost.Value > maxCost.Value)
                (minCost, maxCost) = (maxCost, minCost);

            decimal? minBase = f.MinBaseCost;
            decimal? maxBase = f.MaxBaseCost;
            if (minBase.HasValue && maxBase.HasValue && minBase.Value > maxBase.Value)
                (minBase, maxBase) = (maxBase, minBase);

            if (minCost.HasValue)
                q = q.Where(x => EF.Property<decimal?>(x, "CostValue") >= minCost.Value);
            if (maxCost.HasValue)
                q = q.Where(x => EF.Property<decimal?>(x, "CostValue") <= maxCost.Value);
            if (minBase.HasValue)
                q = q.Where(x => EF.Property<decimal?>(x, "BaseCostValue") >= minBase.Value);
            if (maxBase.HasValue)
                q = q.Where(x => EF.Property<decimal?>(x, "BaseCostValue") <= maxBase.Value);

            return await q
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .Select(x => new ListOfferCalcInfo
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    BaseCost = EF.Property<decimal?>(x, "BaseCostValue") ?? 0m,
                    Cost = EF.Property<decimal?>(x, "CostValue") ?? 0m,
                    Comment = x.Comment ?? string.Empty,
                    Date = x.Date,
                    Organisation = x.Organisation != null ? x.Organisation.Name : string.Empty,
                    Category = x.Organisation != null && x.Organisation.OrganisationCategory != null && x.Organisation.OrganisationCategory.ParentCategory != null
                        ? x.Organisation.OrganisationCategory.ParentCategory.Name
                        : string.Empty,
                    SubCategory = x.Organisation != null && x.Organisation.OrganisationCategory != null
                        ? x.Organisation.OrganisationCategory.Name
                        : string.Empty,
                    ResName = x.Resource.Name,
                    TaskName = x.Resource.Task.Name,
                    TaskCode = x.Resource.Task.Metadata.Code,
                    CalcName = x.Resource.Task.Calculation.Name,
                    CalcCode = x.Resource.Task.Calculation.Code
                })
                .ToListAsync(ct);
        }

// ---------------- Hub notifications (unified) ----------------

        private Task NotifyOfferAsync(int calcId, OperationType op, int parentId, object data)
            => hub.SendNotificationAsync(
                calcId.ToString(),
                ObjectTypHub.Offer,
                op,
                new HubDataDto
                {
                    ParentId = parentId,
                    Data = data
                }
            );

        // ---------------- Projections (DRY + nullable-safe) ----------------

        private static System.Linq.Expressions.Expression<Func<OfferEntity, ListOfferDTO>> ProjectListOfferDto() =>
            x => new ListOfferDTO
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BaseCost = x.Metadata.BaseCost,
                Cost = x.Metadata.Cost,
                Organisation = x.Organisation != null ? x.Organisation.Name : string.Empty,
                Comment = x.Comment ?? string.Empty,
                Date = x.Date,
                OrganisationId = x.OrganisationId,
                SubCategory = x.Organisation != null && x.Organisation.OrganisationCategory != null ? x.Organisation.OrganisationCategory.Name : string.Empty,
                Category = x.Organisation != null && x.Organisation.OrganisationCategory != null && x.Organisation.OrganisationCategory.ParentCategory != null ? x.Organisation.OrganisationCategory.ParentCategory.Name : string.Empty
            };

        private static System.Linq.Expressions.Expression<Func<OfferEntity, OfferWithCalcId>> ProjectOfferWithCalcId() =>
            x => new OfferWithCalcId(
                x.Resource!.Task!.CalculationId,
                new ListOfferDTO
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    BaseCost = x.Metadata.BaseCost,
                    Cost = x.Metadata.Cost,
                    Organisation = x.Organisation != null ? x.Organisation.Name : string.Empty,
                    Comment = x.Comment ?? string.Empty,
                    Date = x.Date,
                    OrganisationId = x.OrganisationId,
                    SubCategory = x.Organisation != null && x.Organisation.OrganisationCategory != null ? x.Organisation.OrganisationCategory.Name : string.Empty,
                    Category = x.Organisation != null && x.Organisation.OrganisationCategory != null && x.Organisation.OrganisationCategory.ParentCategory != null ? x.Organisation.OrganisationCategory.ParentCategory.Name : string.Empty
                }
            );


        private sealed record OfferWithCalcId(int CalcID, ListOfferDTO Offer);
    }
}
using Application.Feature.Account;
using Application.Mapping.Account;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.General;

namespace Persistence.Service.ResourceAccount
{
    public sealed class AccountService(IDbContextFactoryTenant dbFactory) : IAccountService
    {
        public async Task<List<AccountManageDTO>> GetAccountsByGroupAsync(int groupId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Accounts
                .AsNoTracking()
                .Where(x => x.AccountGroupId == groupId)
                .OrderBy(x => x.Code)
                .Select(AccountDtoMapper.ProjectManageDto())
                .ToListAsync(ct);
        }

        public async Task<List<AccountManageDTO>> GetAccountsOverviewAsync(CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Accounts
                .AsNoTracking()
                .OrderBy(x => x.AccountGroup.Name)
                .ThenBy(x => x.Code)
                .Select(x => new AccountManageDTO
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    IsVisible = x.IsVisible,
                    Metadata = x.Metadata,
                    AccountGroupId = x.AccountGroupId,
                    AccountGroupName = x.AccountGroup.Name
                })
                .ToListAsync(ct);
        }

        public async Task<List<ListDTO>> GetAccountsAsListAsync(int groupId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Accounts
                .AsNoTracking()
                .Where(x => x.AccountGroupId == groupId)
                .OrderBy(x => x.Code)
                .Select(x => new ListDTO
                {
                    Id = x.Id,
                    Name = x.Code + " - " + x.Name
                })
                .ToListAsync(ct);
        }

        public async Task<int> CreateAsync(PostAccountDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (!await context.AccountGroup.AsNoTracking().AnyAsync(x => x.Id == dto.AccountGroupId, ct))
                return 0;

            var normalizedCode = NormalizeCode(dto.Account);
            if (await context.Accounts.AsNoTracking().AnyAsync(x => x.Code == normalizedCode, ct))
                return 0;

            var entity = new AccountEntity(
                code: dto.Account,
                name: dto.Name,
                accountGroupId: dto.AccountGroupId,
                isVisible: dto.IsVisible,
                data: dto.ToData()
            );

            context.Accounts.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, PostAccountDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (!await context.AccountGroup.AsNoTracking().AnyAsync(x => x.Id == dto.AccountGroupId, ct))
                return false;

            var normalizedCode = NormalizeCode(dto.Account);
            if (await context.Accounts.AsNoTracking().AnyAsync(x => x.Id != id && x.Code == normalizedCode, ct))
                return false;

            var existing = await context.Accounts.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (existing is null) return false;

            existing.Update(
                code: dto.Account,
                name: dto.Name,
                groupId: dto.AccountGroupId,
                isVisible: dto.IsVisible,
                data: dto.ToData()
            );

            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var isUsedByResources = await context.Resources
                .AsNoTracking()
                .AnyAsync(x => x.AccountId == id, ct);

            var isUsedByResourceTypes = await context.ResourceTypes
                .AsNoTracking()
                .AnyAsync(x => x.AccountId == id, ct);

            var isUsedByResourceSorts = await context.ResourceSorts
                .AsNoTracking()
                .AnyAsync(x => x.AccountId == id, ct);

            if (isUsedByResources || isUsedByResourceTypes || isUsedByResourceSorts)
                return false;

            var existing = await context.Accounts
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (existing is null) return false;

            context.Accounts.Remove(existing);
            await context.SaveChangesAsync(ct);
            return true;
        }

        private static string NormalizeCode(string? code)
            => string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim();
    }
}

using Application.Feature.Account;
using Domain.Entities.Calculation;
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
                .Select(x => new AccountManageDTO
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    IsVisible = x.IsVisible,
                    Metadata = x.Metadata,
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

            var entity = new AccountEntity(
                code: dto.Account,
                name: dto.Name,
                accountGroupId: dto.AccountGroupId,
                isVisible: dto.IsVisible,
                data: dto.Data
            );

            context.Accounts.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, PostAccountDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var existing = await context.Accounts.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (existing is null) return false;

            existing.Update(
                code: dto.Account,
                name: dto.Name,
                groupId: dto.AccountGroupId,
                isVisible: dto.IsVisible,
                data: dto.Data
            );

            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var existing = await context.Accounts.FindAsync([id], ct);
            if (existing is null) return false;

            context.Accounts.Remove(existing);
            await context.SaveChangesAsync(ct);
            return true;
        }
    }
}

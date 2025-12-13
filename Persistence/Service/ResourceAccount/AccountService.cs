using Application.Feature.Account;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.General;

namespace Persistence.Service.ResourceAccount
{
    public sealed class AccountService(ShardingSingleDbContext db) : IAccountService
    {
        public Task<List<ListAccountDTO>> GetAccountsByGroupAsync(int groupId, CancellationToken ct = default)
        {
            return db.Accounts
                .AsNoTracking()
                .Where(x => x.AccountGroupId == groupId)
                .OrderBy(x => x.Code)
                .Select(x => new ListAccountDTO
                {
                    Id = x.Id,
                    Account = x.Code,
                    Name = x.Name
                })
                .ToListAsync(ct);
        }

        public Task<List<ListDTO>> GetAccountsAsListAsync(int groupId, CancellationToken ct = default)
        {
            return db.Accounts
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
            var entity = new AccountEntity(
                code: dto.Account,
                name: dto.Name,
                accountGroupId: dto.AccountGroupId,
                isVisible: dto.IsVisible,
                data: dto.Data
            );

            db.Accounts.Add(entity);
            await db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, PostAccountDTO dto, CancellationToken ct = default)
        {
            var existing = await db.Accounts.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (existing is null) return false;

            existing.Update(
                code: dto.Account,
                name: dto.Name,
                groupId: dto.AccountGroupId,
                isVisible: dto.IsVisible,
                data: dto.Data
            );

            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var existing = await db.Accounts.FindAsync([id], ct);
            if (existing is null) return false;

            db.Accounts.Remove(existing);
            await db.SaveChangesAsync(ct);
            return true;
        }
    }
}

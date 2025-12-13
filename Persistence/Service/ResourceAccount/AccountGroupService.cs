using Application.Feature.Account;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.General;

namespace Persistence.Service.ResourceAccount
{
    public sealed class AccountGroupService(ShardingSingleDbContext db) : IAccountGroupService
    {
        public Task<List<ListDTO>> GetGroupsAsListAsync(CancellationToken ct = default)
        {
            return db.AccountGroup
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new ListDTO
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync(ct);
        }

        public Task<List<ListAccountGroupIncludeAccountDTO>> GetGroupsWithAccountsAsync(CancellationToken ct = default)
        {
            return db.AccountGroup
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new ListAccountGroupIncludeAccountDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Accounts = x.Accounts
                        .OrderBy(a => a.Code) // ✅ بدل OrderByDescending(y => y)
                        .Select(a => new ListAccountDTO
                        {
                            Id = a.Id,
                            Account = a.Code,
                            Name = a.Name
                        })
                        .ToList()
                })
                .ToListAsync(ct);
        }
        public async Task<int> CreateAsync(PostAccountGroupDTO dto, CancellationToken ct = default)
        {
            var entity = new AccountGroupEntity(dto.Name);
            db.AccountGroup.Add(entity);
            await db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<List<int>> CreateRangeAsync(List<PostAccountGroupWithAccountsDTO> items, CancellationToken ct = default)
        {
            var groups = new List<AccountGroupEntity>();

            foreach (var item in items)
            {
                var group = new AccountGroupEntity(item.Name);
                if (item.Accounts != null)
                {
                    foreach (var acc in item.Accounts)
                    {
                        var account = new AccountEntity(acc.Account, acc.Name, 0, acc.IsVisible, acc.Data);
                        group.Accounts.Add(account);
                    }
                }

                groups.Add(group);
            }

            db.AccountGroup.AddRange(groups);
            await db.SaveChangesAsync(ct);
            return groups.Select(x => x.Id).ToList();
        }

        public async Task<bool> UpdateAsync(int id, PostAccountGroupDTO dto, CancellationToken ct = default)
        {
            var existing = await db.AccountGroup.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (existing is null) return false;

            existing.Update(dto.Name);
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var existing = await db.AccountGroup.FindAsync([id], ct);
            if (existing is null) return false;

            db.AccountGroup.Remove(existing);
            await db.SaveChangesAsync(ct);
            return true;
        }
    }

}

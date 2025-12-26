using Application.Feature.Account;
using Domain.Entities.Calculation;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.General;

namespace Persistence.Service.ResourceAccount
{
    public sealed class AccountGroupService(IDbContextFactoryTenant dbFactory) : IAccountGroupService
    {
        public async Task<List<ListDTO>> GetGroupsAsListAsync(CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.AccountGroup
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new ListDTO
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync(ct);
        }

        public async Task<List<ListAccountGroupIncludeAccountDTO>> GetGroupsWithAccountsAsync(CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.AccountGroup
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
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var entity = new AccountGroupEntity(dto.Name);
            context.AccountGroup.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<List<int>> CreateRangeAsync(List<PostAccountGroupWithAccountsDTO> items, CancellationToken ct = default)
        {
            var groups = new List<AccountGroupEntity>();
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            foreach (var item in items)
            {
                var group = new AccountGroupEntity(item.Name);
                if (item.Accounts != null)
                {
                    foreach (var acc in item.Accounts)
                    {
                        var account = new AccountEntity(acc.Account, acc.Name, 0, acc.IsVisible, acc.Data);
                        group.AddAccount(account);
                    }
                }

                groups.Add(group);
            }

            context.AccountGroup.AddRange(groups);
            await context.SaveChangesAsync(ct);
            return groups.Select(x => x.Id).ToList();
        }

        public async Task<bool> UpdateAsync(int id, PostAccountGroupDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var existing = await context.AccountGroup.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (existing is null) return false;

            existing.Update(dto.Name);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var existing = await context.AccountGroup.FindAsync([id], ct);
            if (existing is null) return false;

            context.AccountGroup.Remove(existing);
            await context.SaveChangesAsync(ct);
            return true;
        }
    }

}

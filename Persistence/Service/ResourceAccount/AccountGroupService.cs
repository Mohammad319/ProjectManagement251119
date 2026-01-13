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

            var groups = await context.AccountGroup
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .Select(g => new ListAccountGroupIncludeAccountDTO
                {
                    Id = g.Id,
                    Name = g.Name,
                    Accounts = new List<ListAccountDTO>()
                })
                .ToListAsync(ct);

            if (groups.Count == 0)
                return groups;

            var groupIds = groups.Select(g => g.Id).ToList();

            var accounts = await context.Accounts
                .AsNoTracking()
                .Where(a => groupIds.Contains(a.AccountGroupId))
                .OrderBy(a => a.Code)
                .Select(a => new
                {
                    a.AccountGroupId,
                    Dto = new ListAccountDTO
                    {
                        Id = a.Id,
                        Account = a.Code,
                        Name = a.Name
                    }
                })
                .ToListAsync(ct);

            var accByGroup = accounts
                .GroupBy(x => x.AccountGroupId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());

            foreach (var g in groups)
                if (accByGroup.TryGetValue(g.Id, out var list))
                    g.Accounts = list;

            return groups;
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

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

        public async Task<AccountImportResultDTO> ImportAsync(List<PostAccountGroupWithAccountsDTO> items, bool updateExisting, CancellationToken ct = default)
        {
            var result = new AccountImportResultDTO();
            if (items is null || items.Count == 0)
            {
                result.Success = true;
                return result;
            }

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // Phase 1 — reuse existing groups by name (tenant scoped by the global query filter) and create
            // any that are missing, then save so new groups get their Ids before accounts reference them.
            var groupByName = new Dictionary<string, AccountGroupEntity>(StringComparer.OrdinalIgnoreCase);
            foreach (var existing in await context.AccountGroup.ToListAsync(ct))
                groupByName.TryAdd(existing.Name.Trim(), existing);

            foreach (var item in items)
            {
                var name = (item.Name ?? string.Empty).Trim();
                if (name.Length == 0 || groupByName.ContainsKey(name))
                    continue;

                var group = new AccountGroupEntity(name);
                context.AccountGroup.Add(group);
                groupByName[name] = group;
                result.GroupsCreated++;
            }

            if (result.GroupsCreated > 0)
                await context.SaveChangesAsync(ct);

            // Phase 2 — create/skip/update accounts by code within their group.
            var groupIds = groupByName.Values.Select(g => g.Id).ToList();
            var seen = new Dictionary<(int GroupId, string Code), AccountEntity>();
            foreach (var acc in await context.Accounts.Where(a => groupIds.Contains(a.AccountGroupId)).ToListAsync(ct))
                seen.TryAdd((acc.AccountGroupId, acc.Code.Trim().ToLowerInvariant()), acc);

            foreach (var item in items)
            {
                var name = (item.Name ?? string.Empty).Trim();
                if (name.Length == 0 || !groupByName.TryGetValue(name, out var group))
                    continue;

                foreach (var acc in item.Accounts ?? [])
                {
                    var code = (acc.Account ?? string.Empty).Trim();
                    var accName = (acc.Name ?? string.Empty).Trim();
                    if (code.Length == 0 || accName.Length == 0)
                    {
                        result.AccountsSkipped++;
                        continue;
                    }

                    var key = (group.Id, code.ToLowerInvariant());
                    if (seen.TryGetValue(key, out var existingAccount))
                    {
                        if (updateExisting)
                        {
                            existingAccount.Update(code, accName, group.Id, acc.IsVisible, acc.Data);
                            result.AccountsUpdated++;
                        }
                        else
                        {
                            result.AccountsSkipped++;
                        }

                        continue;
                    }

                    var created = new AccountEntity(code, accName, group.Id, acc.IsVisible, acc.Data);
                    context.Accounts.Add(created);
                    seen[key] = created;
                    result.AccountsCreated++;
                }
            }

            await context.SaveChangesAsync(ct);
            result.Success = true;
            return result;
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

            var hasAccounts = await context.Accounts
                .AsNoTracking()
                .AnyAsync(x => x.AccountGroupId == id, ct);

            if (hasAccounts)
                return false;

            var existing = await context.AccountGroup
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (existing is null) return false;

            context.AccountGroup.Remove(existing);
            await context.SaveChangesAsync(ct);
            return true;
        }
    }

}

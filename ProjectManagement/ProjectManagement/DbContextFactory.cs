using AuthPermissions.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Persistence.Context;
using Persistence.Factory;
using ProjectManagement.Shared.Constant;
using System.Security.Claims;

namespace ProjectManagement
{
    public class DbContextFactory(
        IHttpContextAccessor httpContextAccessor,
        ApplicationDbContext appContext,
        IMemoryCache cache) : IDbContextFactory
    {
        private const string CacheKey = "TenantConnections";

        private string? GetConnectionString(int tenantId)
        {
            if (!cache.TryGetValue<Dictionary<int, string>>(CacheKey, out var tenantConnections))
            {
                tenantConnections = appContext.Tenants
                    .Include(t => t.TenantDB)
                    .Where(t => t.TenantDB != null)
                    .ToDictionary(
                        t => t.Id,
                        t => t.TenantDB!.ConnectionString);

                cache.Set(CacheKey, tenantConnections, TimeSpan.FromDays(10));
            }

            return tenantConnections.TryGetValue(tenantId, out var conn) ? conn : null;
        }

        public ShardingSingleDbContext CreateDbContext()
        {
            var httpContext = httpContextAccessor.HttpContext
                               ?? throw new Exception("No HttpContext available.");

            var user = httpContext.User;

            // TenantId من الـ Claims
            var tenantIdClaim = user.FindFirst(PMClaimsConst.Tentan)?.Value;
            if (!int.TryParse(tenantIdClaim, out var tenantId))
                throw new Exception("Invalid or missing tenantId in user claims.");

            // CreatedAt من الـ Claims
            int? userId = null;
            if (int.TryParse(user.Claims.FirstOrDefault(x => x.Type == PMClaimsConst.UserId)?.Value, out var uId))
                userId = uId;

            // DepartmentId من الـ Claims (لو حبيت تستعمله لاحقاً)
            int? departmentId = null;
            if (int.TryParse(user.Claims.FirstOrDefault(x => x.Type == ClaimTypes.GroupSid)?.Value, out var depId))
                departmentId = depId;

            var connectionString = GetConnectionString(tenantId)
                                   ?? throw new Exception($"No connection string found for tenant {tenantId}.");

            var optionsBuilder = new DbContextOptionsBuilder<ShardingSingleDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            var db = new ShardingSingleDbContext(optionsBuilder.Options)
            {
                TenantId = tenantId,
                CurrentUserId = userId
            };

            return db;
        }
    }

}

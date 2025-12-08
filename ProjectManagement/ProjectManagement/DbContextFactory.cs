using Application.Interfaces.Context;
using AuthPermissions.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Persistence.Context;
using Persistence.Factory;
using ProjectManagement.Shared.Constant;
using System.Security.Claims;

namespace ProjectManagement
{
    public class DbContextFactory : IDbContextFactory
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _appContext;
        private readonly IMemoryCache _cache;

        private const string CacheKey = "TenantConnections";

        public int? TenantID => throw new NotImplementedException();

        public DbContextFactory(
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext appContext,
            IMemoryCache cache)
        {
            _httpContextAccessor = httpContextAccessor;
            _appContext = appContext;
            _cache = cache;
        }

        private string? GetConnectionString(int tenantId)
        {
            if (!_cache.TryGetValue<Dictionary<int, string>>(CacheKey, out var tenantConnections))
            {
                tenantConnections = _appContext.Tenants
                    .Include(t => t.TenantDB)
                    .Where(t => t.TenantDB != null)
                    .ToDictionary(
                        t => t.Id,
                        t => t.TenantDB!.ConnectionString);

                _cache.Set(CacheKey, tenantConnections, TimeSpan.FromDays(10));
            }

            return tenantConnections.TryGetValue(tenantId, out var conn) ? conn : null;
        }

        public IShardingSingleDbContext CreateDbContext()
        {
            var httpContext = _httpContextAccessor.HttpContext
                               ?? throw new Exception("No HttpContext available.");

            var user = httpContext.User;

            // TenantId من الـ Claims
            var tenantIdClaim = user.FindFirst(PMClaimsConst.Tentan)?.Value;
            if (!int.TryParse(tenantIdClaim, out var tenantId))
                throw new Exception("Invalid or missing tenantId in user claims.");

            // UserId من الـ Claims
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

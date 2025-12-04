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
    public class DbContextFactory(IHttpContextAccessor httpContextAccessor, ApplicationDbContext _context, IMemoryCache _cache) : IDbContextFactory
    {
        public int? TenantID { get; private set; }
        public int? DepartmentID { get; private set; }
        public int UserID { get; private set; }
        private const string CacheKey = "TenantConnections";

        public string GetConnectionString(int tenantId)
        {
            var TenantConnections = _cache.Get<Dictionary<int, string>>(CacheKey);
            if (TenantConnections == null)
            {
                TenantConnections = _context.Tenants
                    .Include(t => t.TenantDB) // Eager load the related entity
                    .Where(t => t.TenantDB != null)
                    .ToDictionary(t => t.Id, t => t.TenantDB.ConnectionString);
                _cache.Set(CacheKey, TenantConnections, TimeSpan.FromDays(10));
            }
            return TenantConnections.TryGetValue(tenantId, out var conn) ? conn : null;
        }
        public IShardingSingleDbContext CreateDbContext()
        {
            var user = httpContextAccessor.HttpContext?.User;
            var tenantIdClaim = user?.FindFirst(PMClaimsConst.Tentan)?.Value;
            if (int.TryParse(user?.Claims.FirstOrDefault(x => x.Type == PMClaimsConst.UserId)?.Value, out int userid))
                UserID = userid;
            if (int.TryParse(user?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.GroupSid)?.Value, out int departmentID))
                DepartmentID = departmentID;

            if (!int.TryParse(tenantIdClaim, out var tenantId))
            {
                throw new Exception("Invalid or missing tenantId in user claims.");
            }

            TenantID = tenantId;

            var ConnectionString = GetConnectionString(tenantId);
            var optionsBuilder = new DbContextOptionsBuilder<ShardingSingleDbContext>();
            optionsBuilder.UseSqlServer(ConnectionString);

            var db = new ShardingSingleDbContext(optionsBuilder.Options)
            {
                TenantId = tenantId
            };

            return db;
        }
    }
}

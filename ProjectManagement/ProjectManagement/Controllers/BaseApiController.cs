global using Asp.Versioning;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Server.Controllers.Filters;
using ProjectManagement.Services;
using ProjectManagement.Shared.Constant;
using System.Collections;
using System.Security.Cryptography;
using System.Text;

namespace ProjectManagement.Server.Controllers
{
    [ApiController]
    [ApiExceptionFilter]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class BaseApiController : ControllerBase
    {
        protected int? GetTenantId()
        {
            var tenantContext = HttpContext.RequestServices.GetService<TenantContext>();
            if (tenantContext?.TenantId > 0)
                return tenantContext.TenantId;

            var tenantIdClaim = User?.FindFirst(PMClaimsConst.Tenant)?.Value;
            return int.TryParse(tenantIdClaim, out var tenantId) && tenantId > 0 ? tenantId : null;
        }

        protected int? GetDepartmentId()
        {
            var tenantContext = HttpContext.RequestServices.GetService<TenantContext>();
            if (tenantContext?.DepartmentId > 0)
                return tenantContext.DepartmentId;

            return int.TryParse(User.Claims.FirstOrDefault(x => x.Type == PMClaimsConst.DepartmentId)?.Value, out var dId) && dId > 0
                ? dId
                : null;
        }

        protected int GetUserId()
        {
            var tenantContext = HttpContext.RequestServices.GetService<TenantContext>();
            if (tenantContext?.UserId > 0)
                return tenantContext.UserId.Value;

            return int.TryParse(User.Claims.FirstOrDefault(x => x.Type == PMClaimsConst.UserId)?.Value, out var uId) && uId > 0
                ? uId
                : 0;
        }

        protected bool CanUseTargetDepartment(int targetDepartmentId)
        {
            if (targetDepartmentId <= 0)
                return false;

            var currentDepartmentId = GetDepartmentId();
            if (!currentDepartmentId.HasValue || currentDepartmentId.Value == targetDepartmentId)
                return true;

            return User.IsInRole(PMRolesConst.Tenant.Admin) ||
                   User.IsInRole(PMRolesConst.Tenant.Manger);
        }

        /// <summary>
        /// True när aktuell användare har rollen Visare (TenantViewer) – en läs-/kommenteringsroll
        /// som bara får se projekt/kalkyler som delats med användaren eller avdelningen. Skickas till
        /// åtkomstfiltret (isViewer) så att Visare aldrig ser odelade/privata objekt.
        /// </summary>
        protected bool IsViewer() => User.IsInRole(PMRolesConst.Tenant.Viewer);

        private ICommandDispatcher? _dispatcher;
        protected ICommandDispatcher MicroBus => _dispatcher ??= HttpContext.RequestServices.GetRequiredService<ICommandDispatcher>();

        /// <summary>
        /// Optional REST concurrency support:
        /// If the client sends If-Match header and the DTO has a string property named 'RowVersion',
        /// we copy the ETag value into dto.RowVersion (only when RowVersion is empty).
        /// Expected ETag format: "base64" or W/"base64".
        /// </summary>
        protected void TrySetRowVersionFromIfMatch(object? dto)
        {
            if (dto is null) return;

            var ifMatch = Request?.Headers.IfMatch.ToString();
            if (string.IsNullOrWhiteSpace(ifMatch)) return;

            var token = NormalizeEtagToken(ifMatch);
            if (string.IsNullOrWhiteSpace(token)) return;

            var prop = dto.GetType().GetProperty("RowVersion");
            if (prop is null || prop.PropertyType != typeof(string) || !prop.CanWrite) return;

            var current = prop.GetValue(dto) as string;
            if (!string.IsNullOrWhiteSpace(current)) return;

            prop.SetValue(dto, token);
        }

        /// <summary>
        /// Sends ETag in the response when the payload contains RowVersion.
        /// - For single objects: ETag = "{RowVersion}"
        /// - For collections: weak ETag based on a SHA-256 hash of all RowVersion tokens
        /// This enables If-Match usage without including RowVersion in the request body.
        /// </summary>
        protected void TrySetETag(object? payload)
        {
            if (payload is null) return;

            // Single object
            if (TryGetRowVersionToken(payload, out var token))
            {
                SetETag(token, weak: false);
                return;
            }

            // Collection
            if (payload is IEnumerable enumerable && payload is not string)
            {
                var tokens = new List<string>();
                foreach (var item in enumerable)
                {
                    if (item is null) continue;
                    if (TryGetRowVersionToken(item, out var t)) tokens.Add(t);
                }

                if (tokens.Count == 0) return;

                var joined = string.Join(":", tokens);
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(joined));
                var etagToken = Convert.ToBase64String(hash);
                SetETag(etagToken, weak: true);
            }
        }

        protected void SetETag(string token, bool weak)
        {
            if (string.IsNullOrWhiteSpace(token)) return;
            Response.Headers.ETag = weak ? $"W/\"{token}\"" : $"\"{token}\"";
        }

        private static bool TryGetRowVersionToken(object obj, out string token)
        {
            token = string.Empty;

            var prop = obj.GetType().GetProperty("RowVersion");
            if (prop is null || !prop.CanRead) return false;

            var value = prop.GetValue(obj);
            switch (value)
            {
                case string s when !string.IsNullOrWhiteSpace(s):
                    token = s;
                    return true;

                case byte[] bytes when bytes.Length > 0:
                    token = Convert.ToBase64String(bytes);
                    return true;

                default:
                    return false;
            }
        }

        private static string NormalizeEtagToken(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            // If-Match can contain multiple values: "a", "b". We take the first non-empty.
            var first = raw.Split(',').Select(x => x.Trim()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            if (string.IsNullOrWhiteSpace(first)) return string.Empty;

            var token = first.Trim();
            if (token.StartsWith("W/")) token = token[2..].Trim();
            token = token.Trim().Trim('"');

            return token;
        }
    }
}

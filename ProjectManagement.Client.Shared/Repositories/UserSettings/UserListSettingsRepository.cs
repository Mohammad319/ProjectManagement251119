using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared.DTO.UserSettings;

namespace ProjectManagement.Client.Shared.Repositories.UserSettings
{
    /// <summary>
    /// Talks to the per-user list-settings API. The server scopes everything to the
    /// authenticated user/tenant, so the client only passes scope/kind/payload.
    /// </summary>
    public sealed class UserListSettingsRepository(HTTPRepository http)
    {
        private static string Base => PMAPIConst.UserListSettings;

        public Task<List<UserListSettingDTO>> GetByScopeAsync(string scope, CancellationToken ct = default)
            => http.GetAsync<List<UserListSettingDTO>>(Base + $"?scope={Uri.EscapeDataString(scope)}", ct);

        public Task<bool> UpsertAsync(string scope, string kind, string payload, CancellationToken ct = default)
            => http.PutAsync(new UserListSettingDTO { Scope = scope, Kind = kind, Payload = payload }, Base, ct);
    }
}

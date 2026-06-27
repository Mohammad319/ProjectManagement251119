namespace ProjectManagement.Client.Handless;

using System.Threading;

/// <summary>
/// Per-user (scoped) coordinator that lets the auth-recovery redirect run exactly once.
/// <para>
/// When a session goes bad, the workspace fires many API calls at once and they all come back
/// 401. Without coordination each 401 would trigger its own full-page <c>NavigateTo(..., forceLoad)</c>
/// to <c>/auth/refresh</c>, producing a burst of identical requests that the server rate-limits with
/// HTTP 429 (and a visible "flicker" between states). The first 401 to call
/// <see cref="TryBeginRedirect"/> wins and performs the single navigation; all concurrent callers get
/// <c>false</c> and simply stand down.
/// </para>
/// Scoped (not static) so it is isolated per user: in WebAssembly the scope spans the app instance and
/// resets on the full-page reload that the redirect itself causes; under a Server circuit it stays
/// per-circuit instead of leaking across users.
/// </summary>
public sealed class AuthRedirectState
{
    private int _redirecting;

    /// <summary>Returns <c>true</c> for the first caller only; subsequent callers get <c>false</c>.</summary>
    public bool TryBeginRedirect() => Interlocked.Exchange(ref _redirecting, 1) == 0;
}

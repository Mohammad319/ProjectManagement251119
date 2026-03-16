namespace ProjectManagement.Shared.Helper;

public static class AuthRecoveryPathHelper
{
    public const string RetryQueryKey = "authRetry";
    private const string RetryQueryValue = "1";

    public static bool HasRetryFlag(string? localUrl)
        => GetQueryTokens(NormalizeLocalUrl(localUrl))
            .Any(IsRetryToken);

    public static string AppendRetryFlag(string? localUrl)
    {
        var normalized = NormalizeLocalUrl(localUrl);
        if (HasRetryFlag(normalized))
            return normalized;

        var (pathAndQuery, fragment) = SplitFragment(normalized);
        var separator = pathAndQuery.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{pathAndQuery}{separator}{RetryQueryKey}={RetryQueryValue}{fragment}";
    }

    public static string RemoveRetryFlag(string? localUrl)
    {
        var normalized = NormalizeLocalUrl(localUrl);
        var (pathAndQuery, fragment) = SplitFragment(normalized);
        var queryIndex = pathAndQuery.IndexOf('?');

        if (queryIndex < 0)
            return normalized;

        var path = pathAndQuery[..queryIndex];
        var remainingTokens = GetQueryTokens(pathAndQuery)
            .Where(token => !IsRetryToken(token))
            .ToArray();

        if (remainingTokens.Length == 0)
            return $"{path}{fragment}";

        return $"{path}?{string.Join("&", remainingTokens)}{fragment}";
    }

    public static string BuildRefreshUrl(string? localUrl)
        => $"/auth/refresh?returnUrl={Uri.EscapeDataString(AppendRetryFlag(localUrl))}";

    public static string BuildLoginUrl(string? localUrl)
        => $"/Account/Login?returnUrl={Uri.EscapeDataString(RemoveRetryFlag(localUrl))}";

    public static string NormalizeLocalUrl(string? localUrl)
    {
        var candidate = string.IsNullOrWhiteSpace(localUrl) ? "/" : localUrl.Trim();
        return candidate.StartsWith('/') ? candidate : $"/{candidate.TrimStart('/')}";
    }

    private static bool IsRetryToken(string token)
    {
        var key = token.Split('=', 2)[0];
        return string.Equals(Uri.UnescapeDataString(key), RetryQueryKey, StringComparison.OrdinalIgnoreCase);
    }

    private static string[] GetQueryTokens(string localUrl)
    {
        var (pathAndQuery, _) = SplitFragment(localUrl);
        var queryIndex = pathAndQuery.IndexOf('?');
        if (queryIndex < 0 || queryIndex == pathAndQuery.Length - 1)
            return [];

        return pathAndQuery[(queryIndex + 1)..]
            .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static (string PathAndQuery, string Fragment) SplitFragment(string localUrl)
    {
        var fragmentIndex = localUrl.IndexOf('#');
        if (fragmentIndex < 0)
            return (localUrl, string.Empty);

        return (localUrl[..fragmentIndex], localUrl[fragmentIndex..]);
    }
}

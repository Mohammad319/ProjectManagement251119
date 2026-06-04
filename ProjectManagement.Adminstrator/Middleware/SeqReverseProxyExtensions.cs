using System.Net.Http.Headers;

namespace ProjectManagement.Adminstrator.Middleware;

public static class SeqReverseProxyExtensions
{
    private static readonly string[] ProxyMethods =
    [
        "GET",
        "POST",
        "PUT",
        "PATCH",
        "DELETE",
        "HEAD",
        "OPTIONS"
    ];

    public static void MapSeqReverseProxy(this WebApplication app, string roles)
    {
        app.MapMethods("/seq/{**path}", ProxyMethods, ForwardAsync)
            .RequireAuthorization(policy => policy.RequireRole(roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));
    }

    private static async Task ForwardAsync(
        HttpContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        if (string.Equals(context.Request.Path.Value, "/seq", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/seq/", permanent: false);
            return;
        }

        var logger = loggerFactory.CreateLogger("SeqReverseProxy");
        var targetBaseUrl = configuration["SeqProxy:TargetUrl"];
        if (string.IsNullOrWhiteSpace(targetBaseUrl))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Seq proxy target is not configured.");
            return;
        }

        if (!Uri.TryCreate(NormalizeBaseUrl(targetBaseUrl), UriKind.Absolute, out var targetBaseUri))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Seq proxy target is invalid.");
            return;
        }

        using var upstreamRequest = CreateUpstreamRequest(context, targetBaseUri);
        var client = httpClientFactory.CreateClient("SeqProxy");

        try
        {
            using var upstreamResponse = await client.SendAsync(
                upstreamRequest,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted);

            await CopyUpstreamResponseAsync(context, upstreamResponse, targetBaseUri);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Seq proxy could not reach {SeqTargetUrl}", targetBaseUrl);
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await context.Response.WriteAsync("Seq is not reachable. Check that Seq is running and SeqProxy:TargetUrl is correct.");
        }
    }

    private static HttpRequestMessage CreateUpstreamRequest(HttpContext context, Uri targetBaseUri)
    {
        var targetUri = BuildTargetUri(context, targetBaseUri);
        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri);

        if (!HttpMethods.IsGet(context.Request.Method) &&
            !HttpMethods.IsHead(context.Request.Method) &&
            !HttpMethods.IsOptions(context.Request.Method))
        {
            request.Content = new StreamContent(context.Request.Body);
        }

        foreach (var header in context.Request.Headers)
        {
            if (IsHeaderExcluded(header.Key))
                continue;

            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && request.Content is not null)
            {
                request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        request.Headers.Host = targetBaseUri.IsDefaultPort
            ? targetBaseUri.Host
            : $"{targetBaseUri.Host}:{targetBaseUri.Port}";

        request.Headers.TryAddWithoutValidation("X-Forwarded-Host", context.Request.Host.Value);
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", context.Request.Scheme);
        request.Headers.TryAddWithoutValidation("X-Forwarded-PathBase", "/seq");
        request.Headers.TryAddWithoutValidation("X-Forwarded-Prefix", "/seq");

        return request;
    }

    private static Uri BuildTargetUri(HttpContext context, Uri targetBaseUri)
    {
        var pathAfterSeq = context.Request.Path.Value ?? string.Empty;
        if (pathAfterSeq.StartsWith("/seq", StringComparison.OrdinalIgnoreCase))
        {
            pathAfterSeq = pathAfterSeq[4..];
        }

        if (string.IsNullOrEmpty(pathAfterSeq))
        {
            pathAfterSeq = "/";
        }

        var builder = new UriBuilder(targetBaseUri);
        var targetBasePath = builder.Path.TrimEnd('/');
        builder.Path = targetBasePath + pathAfterSeq;
        builder.Query = context.Request.QueryString.HasValue
            ? context.Request.QueryString.Value![1..]
            : string.Empty;

        return builder.Uri;
    }

    private static async Task CopyUpstreamResponseAsync(HttpContext context, HttpResponseMessage upstreamResponse, Uri targetBaseUri)
    {
        context.Response.StatusCode = (int)upstreamResponse.StatusCode;

        foreach (var header in upstreamResponse.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in upstreamResponse.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        context.Response.Headers.Remove("X-Frame-Options");
        context.Response.Headers.Remove("Content-Length");

        if (context.Response.Headers.TryGetValue("Location", out var location))
        {
            context.Response.Headers["Location"] = RewriteLocation(location.ToString(), targetBaseUri);
        }

        await upstreamResponse.Content.CopyToAsync(context.Response.Body);
    }

    private static string RewriteLocation(string location, Uri targetBaseUri)
    {
        if (location.StartsWith(targetBaseUri.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return "/seq/" + location[targetBaseUri.ToString().Length..].TrimStart('/');
        }

        if (location.StartsWith("/", StringComparison.Ordinal) && !location.StartsWith("/seq", StringComparison.OrdinalIgnoreCase))
        {
            return "/seq" + location;
        }

        return location;
    }

    private static bool IsHeaderExcluded(string headerName)
    {
        return string.Equals(headerName, "Host", StringComparison.OrdinalIgnoreCase)
            || string.Equals(headerName, "Connection", StringComparison.OrdinalIgnoreCase)
            || string.Equals(headerName, "Upgrade", StringComparison.OrdinalIgnoreCase)
            || string.Equals(headerName, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeBaseUrl(string url)
    {
        return url.EndsWith("/", StringComparison.Ordinal) ? url : url + "/";
    }
}

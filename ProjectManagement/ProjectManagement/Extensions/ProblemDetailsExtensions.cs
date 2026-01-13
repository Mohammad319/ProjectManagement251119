using Microsoft.AspNetCore.Mvc;

namespace ProjectManagement.Extensions;

public static class ProblemDetailsExtensions
{
    public static ProblemDetails WithTraceId(this ProblemDetails pd, string traceId)
    {
        pd.Extensions["traceId"] = traceId;
        return pd;
    }
}

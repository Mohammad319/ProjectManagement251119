#nullable enable
﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectManagement.Shared.DTO.General
{
    public record ClientLogEvent(
        string Level,          // "Error" | "Warning" | "Information"
        string Message,
        string? TraceId,       // من ProblemDetails أو X-Correlation-ID إن توفر
        string? Url,
        string? UserId,
        string? TenantId,
        string? Exception,     // نص مختصر (اختياري)
        DateTimeOffset? ClientTimeUtc
    );
}

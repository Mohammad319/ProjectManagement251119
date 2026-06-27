using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectManagement.Shared.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Server.Controllers.Filters
{
    /// <summary>
    /// Centralized API exception handling -> RFC7807 ProblemDetails.
    /// Attach it once to BaseApiController to cover all derived controllers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class ApiExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            var http = context.HttpContext;
            var traceId = http.TraceIdentifier;

            var logger = http.RequestServices.GetRequiredService<ILogger<ApiExceptionFilterAttribute>>();
            var env = http.RequestServices.GetRequiredService<IHostEnvironment>();
            var isDev = env.IsDevelopment();

            if (context.Exception is not OperationCanceledException)
                logger.LogError(context.Exception, "Unhandled API exception. TraceId={TraceId}", traceId);

            var ex = context.Exception;

            // Map known exceptions
            var (status, title, type, errorCode, detail) = ex switch
            {
                OperationCanceledException => (
                    StatusCodes.Status408RequestTimeout,
                    "Request cancelled",
                    "https://httpstatuses.com/408",
                    "request_cancelled",
                    "The operation was cancelled."
                ),

                ForbiddenActionException fex => (
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    "https://httpstatuses.com/403",
                    "forbidden_action",
                    fex.Message
                ),

                ConcurrencyConflictException cex => (
                    StatusCodes.Status409Conflict,
                    "Concurrency conflict",
                    "https://httpstatuses.com/409",
                    "concurrency_conflict",
                    cex.Message
                ),

                DbUpdateConcurrencyException => (
                    StatusCodes.Status409Conflict,
                    "Concurrency conflict",
                    "https://httpstatuses.com/409",
                    "concurrency_conflict",
                    "The resource was modified by someone else. Please reload and try again."
                ),

                ValidationException vex => (
                    StatusCodes.Status400BadRequest,
                    "Validation failed",
                    "https://httpstatuses.com/400",
                    "validation_failed",
                    isDev ? vex.Message : "The request data is invalid."
                ),

                KeyNotFoundException knf => (
                    StatusCodes.Status404NotFound,
                    "Not found",
                    "https://httpstatuses.com/404",
                    "not_found",
                    isDev ? knf.Message : "The requested resource was not found."
                ),

                ArgumentException aex => (
                    StatusCodes.Status400BadRequest,
                    "Bad request",
                    "https://httpstatuses.com/400",
                    "bad_request",
                    isDev ? aex.Message : "The request is invalid."
                ),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    "Server error",
                    "https://httpstatuses.com/500",
                    "server_error",
                    isDev ? ex.ToString() : "An unexpected error occurred."
                )
            };

            var pd = new ProblemDetails
            {
                Title = title,
                Status = status,
                Detail = detail,
                Type = type
            };

            pd.Extensions["error"] = errorCode;
            pd.Extensions["traceId"] = traceId;

            context.Result = new ObjectResult(pd) { StatusCode = status };
            context.ExceptionHandled = true;
        }
    }
}

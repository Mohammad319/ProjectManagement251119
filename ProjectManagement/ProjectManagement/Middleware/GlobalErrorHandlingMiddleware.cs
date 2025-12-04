using Serilog;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace ProjectManagement.Server.Middleware
{
    public class GlobalErrorHandling(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (System.Exception ex)
            {
                Log.Error(ex, "Unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, System.Exception exception)
        {
            context.Response.ContentType = "application/json";

            context.Response.StatusCode = exception switch
            {
                ValidationException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var message = exception switch
            {
                ValidationException ve => ve.Message,
                _ => "Internal Server Error."
            };

            await context.Response.WriteAsJsonAsync(new { error = message });
        }
    }
}

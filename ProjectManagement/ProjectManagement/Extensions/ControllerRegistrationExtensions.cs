using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Resource;
using System.Globalization;

namespace ProjectManagement.Extensions;

public static class ControllerRegistrationExtensions
{
    private static string SharedText(string key, string fallback)
        => ResLocalize.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;

    public static IServiceCollection AddProjectControllers(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                var traceId = System.Diagnostics.Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
                ctx.ProblemDetails.Extensions["traceId"] = traceId;
            };
        });

        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var pd = new ValidationProblemDetails(context.ModelState)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = SharedText("ValidationErrorTitle", "Validation error"),
                        Detail = SharedText("InvalidInputDetails", "The submitted data is invalid.")
                    };
                    pd.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                    return new BadRequestObjectResult(pd);
                };
            });

        return services;
    }
}

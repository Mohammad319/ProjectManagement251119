using Microsoft.AspNetCore.Mvc;

namespace ProjectManagement.Extensions;

public static class ControllerRegistrationExtensions
{
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
                        Title = "Validation error",
                        Detail = "البيانات المرسلة غير صحيحة."
                    };
                    pd.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                    return new BadRequestObjectResult(pd);
                };
            });

        return services;
    }
}

using Application;
using AuthPermissions;
using AuthPermissions.Context;
using BlazorMHD.UI.Core.Services;
using Domain.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Components.Account;
using ProjectManagement.Configuration;
using ProjectManagement.Middleware;
using ProjectManagement.Services;
using TaskResourceBlueprints;
using TaskResourceBlueprints.Infrastructure;
using Persistence.Factory;
namespace ProjectManagement.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectManagementApp(
        this IServiceCollection services,
        WebApplicationBuilder builder,
        AppConnectionStrings conn)
    {
        // Settings + Identity helpers
        services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
        services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

        // Layers
        services.AddCustomAuthentication(conn.DefaultConnection);
        services.AddApplicationLayer();
        services.AddPersistenceServices();
        services.AddAuthPermissionsLayer();
        services.AddTaskResourceBlueprints();

        // Dev only
        services.AddDatabaseDeveloperPageExceptionFilter();

        // DbContextFactory (Blueprints)
        services.AddDbContextFactory<TaskResourceBlueprintsContext>(options =>
            options.UseSqlServer(conn.TaskResourceBlueprintsDb, sqlOptions =>
            {
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            }));

        // UI/tenant/services
        services.AddProjectServices();

        // Response compression
        services.AddResponseCompression(opts =>
        {
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
        });

        // Misc
        services.BlazorMHD();
        services.AddHttpContextAccessor();

        // Middleware registrations
        services.AddTransient<CorrelationIdMiddleware>(); // :contentReference[oaicite:8]{index=8}

        // Controllers + model validation response
        services.AddProjectControllers();

        // UI helper
        services.AddScoped<AppErrorDialog>();

        return services;
    }
}

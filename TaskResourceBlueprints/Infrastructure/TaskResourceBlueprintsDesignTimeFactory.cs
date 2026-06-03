using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TaskResourceBlueprints.Infrastructure;

public sealed class TaskResourceBlueprintsDesignTimeFactory : IDesignTimeDbContextFactory<TaskResourceBlueprintsContext>
{
    public TaskResourceBlueprintsContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var conn = configuration.GetConnectionString("BlueprintsConnection")
                   ?? throw new InvalidOperationException(
                       "BlueprintsConnection not found. Run dotnet ef from the startup project directory.");

        var options = new DbContextOptionsBuilder<TaskResourceBlueprintsContext>()
            .UseSqlServer(conn, sql => sql.EnableRetryOnFailure())
            .Options;

        return new TaskResourceBlueprintsContext(options);
    }
}

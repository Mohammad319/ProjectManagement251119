using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskResourceBlueprints.Infrastructure;

public sealed class TaskResourceBlueprintsDesignTimeFactory : IDesignTimeDbContextFactory<TaskResourceBlueprintsContext>
{
    public TaskResourceBlueprintsContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("PM_BLUEPRINT_CONN")
                   ?? @"Data Source=.\SQLEXPRESS;Initial Catalog=TaskResourceBlueprints2;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=True;Application Intent=ReadWrite;MultipleActiveResultSets=True";

        var options = new DbContextOptionsBuilder<TaskResourceBlueprintsContext>()
            .UseSqlServer(conn, sql => sql.EnableRetryOnFailure())
            .Options;

        return new TaskResourceBlueprintsContext(options);
    }
}

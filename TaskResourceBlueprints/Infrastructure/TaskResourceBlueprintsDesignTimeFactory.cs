using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskResourceBlueprints.Infrastructure;

/// <summary>
/// Design-time factory لتوليد migrations لقاعدة بيانات الـ blueprints.
/// استخدم env var: BLUEPRINTS_CONN.
/// </summary>
public sealed class TaskResourceBlueprintsDesignTimeFactory : IDesignTimeDbContextFactory<TaskResourceBlueprintsContext>
{
    public TaskResourceBlueprintsContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("BLUEPRINTS_CONN")
                   ?? @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=TaskResourceBlueprints;Integrated Security=True;MultipleActiveResultSets=True";

        var options = new DbContextOptionsBuilder<TaskResourceBlueprintsContext>()
            .UseSqlServer(conn)
            .Options;

        return new TaskResourceBlueprintsContext(options);
    }
}

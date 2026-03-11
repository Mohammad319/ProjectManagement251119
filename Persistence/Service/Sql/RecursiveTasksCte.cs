using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Domain.Entities.Calculation;

namespace Persistence.Service.Sql;

/// <summary>
/// Composable recursive CTE for loading a task subtree.
///
/// Why: avoids non-composable stored procedures (EXEC GetRecursiveTasks)
/// which bypass global query filters and require manual DB objects.
///
/// Security: TenantId (and optionally CalculationId) is embedded in the CTE.
///
/// Notes:
/// - Keep the SQL fully composable because callers add LINQ operators (e.g., AsNoTracking)
///   on top of this query.
/// </summary>
internal static class RecursiveTasksCte
{
    /// <summary>
    /// Returns a composable query that yields the root task + all descendants.
    /// </summary>
    public static IQueryable<TaskEntity> Query(
        ShardingSingleDbContext db,
        int rootTaskId,
        int? calculationId = null)
    {
        if (db is null) throw new ArgumentNullException(nameof(db));
        if (rootTaskId <= 0) throw new ArgumentOutOfRangeException(nameof(rootTaskId));

        var tenantId = db.TenantId;
        if (tenantId <= 0)
            throw new UnauthorizedAccessException("TenantId is not set on DbContext.");

        // Note: use interpolated parameters to keep it safe & parameterized.
        if (calculationId.HasValue)
        {
            var calcId = calculationId.Value;
            return db.Tasks.FromSqlInterpolated($@"
WITH cte AS (
    SELECT *
    FROM Tasks
    WHERE Id = {rootTaskId}
      AND TenantId = {tenantId}
      AND CalculationId = {calcId}

    UNION ALL

    SELECT t.*
    FROM Tasks t
    INNER JOIN cte c ON t.ParentTaskId = c.Id
    WHERE t.TenantId = {tenantId}
      AND t.CalculationId = {calcId}
)
SELECT * FROM cte");
        }

        return db.Tasks.FromSqlInterpolated($@"
WITH cte AS (
    SELECT *
    FROM Tasks
    WHERE Id = {rootTaskId}
      AND TenantId = {tenantId}

    UNION ALL

    SELECT t.*
    FROM Tasks t
    INNER JOIN cte c ON t.ParentTaskId = c.Id
    WHERE t.TenantId = {tenantId}
)
SELECT * FROM cte");
    }
}

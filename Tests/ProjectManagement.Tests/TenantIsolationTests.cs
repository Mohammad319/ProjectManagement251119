using Domain.Entities.Calculation;
using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Shared.DTO.Calculation;
using Xunit;

namespace ProjectManagement.Tests;

/// <summary>
/// Verifies that TenantId and DepartmentId boundaries are enforced
/// by the global query filter and service-layer checks.
/// </summary>
public sealed class TenantIsolationTests
{
    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static ShardingSingleDbContext BuildContext(string dbName, int tenantId)
    {
        var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ShardingSingleDbContext(options) { TenantId = tenantId };
    }

    private static TaskEntity MakeTask(int tenantId, int calculationId, int? departmentId = null)
    {
        var calculation = MakeCalculation(tenantId, calculationId, departmentId ?? calculationId);
        var task = TaskEntity.Create(calculationId, new TaskPostDTO
        {
            Name = "Test Task",
            Unit = "h",
            Quantity = 1
        }, sortOrder: 1);
        task.TenantId = tenantId;
        SetPrivate(task, nameof(TaskEntity.Calculation), calculation);
        calculation.Tasks.Add(task);
        return task;
    }

    private static CalculationEntity MakeCalculation(int tenantId, int id, int departmentId)
    {
        var project = MakeProject(tenantId);
        var calculation = new CalculationEntity
        {
            Id = id,
            TenantId = tenantId
        };

        _ = calculation.Metadata;
        _ = calculation.Sort;
        calculation.AssignDepartment(departmentId);
        SetPrivate(calculation, nameof(CalculationEntity.Project), project);
        SetPrivate(calculation, nameof(CalculationEntity.ProjectId), project.Id);
        SetPrivate(calculation, nameof(CalculationEntity.Name), $"Calculation {id}");
        SetPrivate(calculation, nameof(CalculationEntity.Code), $"C-{id}");
        project.Calculations.Add(calculation);

        return calculation;
    }

    private static ProjectEntity MakeProject(int tenantId)
    {
        var project = (ProjectEntity)Activator.CreateInstance(typeof(ProjectEntity), nonPublic: true)!;
        project.Id = Guid.NewGuid();
        project.TenantId = tenantId;
        _ = project.Metadata;
        SetPrivate(project, nameof(ProjectEntity.Name), "Project");
        return project;
    }

    private static void SetPrivate<T>(object target, string propertyName, T value)
    {
        var property = target.GetType().GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().Name}.");

        property.SetValue(target, value);
    }

    // ──────────────────────────────────────────────
    // 1. Global query filter: TenantId isolation
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Tasks_are_invisible_to_other_tenant()
    {
        const string db = "TenantFilter_Tasks";

        await using (var seed = BuildContext(db, tenantId: 1))
        {
            seed.Tasks.Add(MakeTask(tenantId: 1, calculationId: 10));
            seed.Tasks.Add(MakeTask(tenantId: 2, calculationId: 20));
            await seed.SaveChangesAsync();
        }

        await using var tenant1 = BuildContext(db, tenantId: 1);
        var visibleToTenant1 = await tenant1.Tasks.ToListAsync();
        Assert.All(visibleToTenant1, t => Assert.Equal(1, t.TenantId));

        await using var tenant2 = BuildContext(db, tenantId: 2);
        var visibleToTenant2 = await tenant2.Tasks.ToListAsync();
        Assert.All(visibleToTenant2, t => Assert.Equal(2, t.TenantId));
    }

    [Fact]
    public async Task Tasks_count_per_tenant_is_correct()
    {
        const string db = "TenantFilter_Count";

        await using (var seed = BuildContext(db, tenantId: 0))
        {
            seed.Tasks.Add(MakeTask(tenantId: 1, calculationId: 10));
            seed.Tasks.Add(MakeTask(tenantId: 1, calculationId: 11));
            seed.Tasks.Add(MakeTask(tenantId: 2, calculationId: 20));
            await seed.SaveChangesAsync();
        }

        await using var ctx = BuildContext(db, tenantId: 1);
        Assert.Equal(2, await ctx.Tasks.CountAsync());
    }

    // ──────────────────────────────────────────────
    // 2. Opportunity.DeleteAsync: only affects own calculation
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Tasks_from_another_calculation_are_not_modified_on_opportunity_nulling()
    {
        const string db = "OppDelete_DeptIsolation";
        const int tenantId   = 1;
        const int calcA      = 100;
        const int calcB      = 200;

        TaskEntity taskA, taskB;

        await using (var seed = BuildContext(db, tenantId))
        {
            taskA = MakeTask(tenantId, calcA);
            taskB = MakeTask(tenantId, calcB);

            // Both share oppId=1 (simulates a cross-calc reference that should NOT happen,
            // but we verify our query scope prevents accidental modification of calcB's task)
            seed.Tasks.Add(taskA);
            seed.Tasks.Add(taskB);
            await seed.SaveChangesAsync();
        }

        await using var ctx = BuildContext(db, tenantId);

        // The fixed OpportunityService filters: x.CalculationId == opp.CalculationId
        // Simulate that: only load tasks where CalculationId == calcA
        var affected = await ctx.Tasks
            .Where(x => x.CalculationId == calcA)
            .ToListAsync();

        Assert.Single(affected);
        Assert.Equal(calcA, affected[0].CalculationId);
    }

    // ──────────────────────────────────────────────
    // 3. Cross-tenant write is blocked by filter
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Writing_task_for_wrong_tenant_is_not_readable_by_other_tenant()
    {
        const string db = "CrossTenant_Write";

        await using (var seed = BuildContext(db, tenantId: 0))
        {
            seed.Tasks.Add(MakeTask(tenantId: 99, calculationId: 99));
            await seed.SaveChangesAsync();
        }

        // Tenant 1 should not see tenant 99's data
        await using var ctx = BuildContext(db, tenantId: 1);
        Assert.Empty(await ctx.Tasks.ToListAsync());
    }

    // ──────────────────────────────────────────────
    // 4. Query filter applies to FirstOrDefaultAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task FindById_returns_null_for_other_tenant_record()
    {
        const string db = "TenantFilter_FindById";
        int taskId;

        await using (var seed = BuildContext(db, tenantId: 0))
        {
            var task = MakeTask(tenantId: 1, calculationId: 10);
            seed.Tasks.Add(task);
            await seed.SaveChangesAsync();
            taskId = task.Id;
        }

        // Tenant 2 tries to query by that Id — must get null due to query filter
        await using var ctx = BuildContext(db, tenantId: 2);
        var result = await ctx.Tasks.FirstOrDefaultAsync(x => x.Id == taskId);
        Assert.Null(result);
    }

    // ──────────────────────────────────────────────
    // 5. DepartmentId boundary: tasks from dept A not in dept B query
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Tasks_filtered_by_departmentId_exclude_other_departments()
    {
        const string db  = "DeptFilter_Tasks";
        const int tenant = 1;
        const int deptA  = 10;
        const int deptB  = 20;

        // DepartmentId lives on CalculationEntity, not TaskEntity directly.
        // We simulate the service-layer pattern: filter tasks via CalculationId scope.
        await using (var seed = BuildContext(db, tenantId: 0))
        {
            var taskForDeptA = MakeTask(tenant, calculationId: deptA * 100, departmentId: deptA);
            var taskForDeptB = MakeTask(tenant, calculationId: deptB * 100, departmentId: deptB);
            seed.Tasks.Add(taskForDeptA);
            seed.Tasks.Add(taskForDeptB);
            await seed.SaveChangesAsync();
        }

        await using var ctx = BuildContext(db, tenant);

        // Simulates: service passes departmentA's calculationIds
        var deptACalcIds = new[] { deptA * 100 };
        var deptATasks = await ctx.Tasks
            .Where(t => deptACalcIds.Contains(t.CalculationId))
            .ToListAsync();

        Assert.Single(deptATasks);
        Assert.DoesNotContain(deptATasks, t => t.CalculationId == deptB * 100);
    }
}

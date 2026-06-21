using Application.Feature.Transfer;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.Context;
using Persistence.Factory;
using Persistence.Service.Transfer;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.DTO.Transfer;
using Xunit;

namespace ProjectManagement.Tests;

/// <summary>
/// End-to-end coverage of the ATACOST external copy feature: export builds a .atacost package
/// (JSON zipped), and import rebuilds a fully standalone project/calculation with fresh IDs.
/// All entities use TenantId 0 so the global query filter matches both seeded and imported rows.
/// Access is exercised as Admin (departmentId = null) so the access rules short-circuit before
/// touching navigations the in-memory store does not need.
/// </summary>
public sealed class AtacostTransferTests
{
    private const int Admin = 1;
    private const int DepartmentId = 7;

    // The factory hands out fresh contexts that share the same in-memory store (keyed by name).
    private sealed class FakeFactory(string dbName) : IDbContextFactoryTenant
    {
        public Task<ShardingSingleDbContext> CreateDbContextAsync(CancellationToken ct = default)
        {
            var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return Task.FromResult(new ShardingSingleDbContext(options) { TenantId = 0 });
        }
    }

    private static ShardingSingleDbContext Open(string dbName)
    {
        var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ShardingSingleDbContext(options) { TenantId = 0 };
    }

    private static Guid SeedFolder(ShardingSingleDbContext db)
    {
        var folder = new FolderEntity("Folder", "#112233", DepartmentId, Admin, 100)
        {
            Id = Guid.NewGuid()
        };
        db.Folders.Add(folder);
        return folder.Id;
    }

    private static ProjectEntity SeedProject(ShardingSingleDbContext db, Guid folderId, string name)
    {
        var project = ProjectEntity.Create(
            new PostProjectDTO { Name = name, Code = name, FolderId = folderId },
            folderId, Admin, 100);
        project.Id = Guid.NewGuid();
        db.Projects.Add(project);
        return project;
    }

    private static CalculationEntity SeedCalculation(
        ShardingSingleDbContext db, Guid projectId, string name, bool isPrivate, int? statusId)
    {
        var calc = new CalculationEntity();
        calc.AssignToProject(projectId);
        calc.AssignDepartment(DepartmentId);
        calc.Update(new CalculationPostDTO
        {
            Name = name,
            Code = name,
            IsPrivate = isPrivate,
            StatusId = statusId
        });
        calc.InitializeVersionGroup();
        calc.CreatedBy = Admin;
        db.Calculations.Add(calc);
        return calc;
    }

    private static void SeedTaskWithResource(ShardingSingleDbContext db, int calcId, decimal cost, int accountId)
    {
        var task = TaskEntity.Create(
            calcId,
            new TaskPostDTO { Name = "Task", Unit = "st", Quantity = 2m, Type = TaskType.Task },
            sortOrder: 100);

        var resource = ResourceEntity.Create(
            new ResourcePostDTO { Name = "Resource", Quantity = 3m, Cost = cost, AccountId = accountId },
            sortOrder: 100,
            parentTaskId: null);

        task.Resources.Add(resource);
        db.Tasks.Add(task);
    }

    [Fact]
    public async Task Project_copy_round_trips_excludes_private_and_clears_tenant_refs()
    {
        const string dbName = "Atacost_Project_RoundTrip";
        var factory = new FakeFactory(dbName);
        var service = new AtacostTransferService(factory, NullLogger<AtacostTransferService>.Instance);

        Guid folderId;
        Guid sourceProjectId;
        int sharedCalcId;
        int privateCalcId;

        await using (var seed = Open(dbName))
        {
            folderId = SeedFolder(seed);
            var project = SeedProject(seed, folderId, "Källprojekt");
            await seed.SaveChangesAsync();

            var shared = SeedCalculation(seed, project.Id, "Delkalkyl", isPrivate: false, statusId: 55);
            var secret = SeedCalculation(seed, project.Id, "Hemlig", isPrivate: true, statusId: 55);
            await seed.SaveChangesAsync();

            SeedTaskWithResource(seed, shared.Id, cost: 500m, accountId: 77);
            await seed.SaveChangesAsync();

            sourceProjectId = project.Id;
            sharedCalcId = shared.Id;
            privateCalcId = secret.Id;
        }

        // Export selecting BOTH calculations; the private one must be excluded by the backend.
        var bytes = await service.BuildProjectPackageAsync(
            sourceProjectId,
            new AtacostProjectExportRequest { CalculationIds = [sharedCalcId, privateCalcId], Message = "Hej" },
            userId: Admin, departmentId: null, isViewer: false);

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes!);

        // Import into the same folder; a new standalone project is always created.
        var result = await service.ImportProjectPackageAsync(
            bytes!, folderId, userId: Admin, departmentId: null, allowCrossDepartment: true);

        Assert.True(result.Success);
        var newProjectId = result.NewProjectId!.Value;
        Assert.NotEqual(Guid.Empty, newProjectId);
        Assert.NotEqual(sourceProjectId, newProjectId);

        await using var verify = Open(dbName);
        var imported = await verify.Calculations
            .Include(c => c.Tasks).ThenInclude(t => t.Resources)
            .Where(c => c.ProjectId == newProjectId)
            .ToListAsync();

        // Only the non-private calculation made it across.
        Assert.Single(imported);
        var calc = imported[0];
        Assert.False(calc.IsPrivate);
        Assert.Null(calc.StatusId); // tenant-specific reference cleared on import

        var task = Assert.Single(calc.Tasks);
        var resource = Assert.Single(task.Resources);
        Assert.Equal(500m, resource.GetMetadataSnapshot().Cost); // economy preserved
        Assert.Null(resource.AccountId);                          // tenant-specific reference cleared
        Assert.True(calc.GetMetadataSnapshot().ImportInfo?.IsImportedCopy);
        Assert.Contains(calc.GetMetadataSnapshot().ImportInfo!.Issues, issue =>
            issue.ProblemType.Contains("Konto", StringComparison.Ordinal));
        Assert.Contains("Originalkonto", resource.GetMetadataSnapshot().ImportInfo);

        // The new calculation has a brand-new id, not the original one.
        Assert.NotEqual(sharedCalcId, calc.Id);
    }

    [Fact]
    public async Task Calculation_copy_round_trips_into_target_project()
    {
        const string dbName = "Atacost_Calc_RoundTrip";
        var factory = new FakeFactory(dbName);
        var service = new AtacostTransferService(factory, NullLogger<AtacostTransferService>.Instance);

        Guid targetProjectId;
        int sourceCalcId;

        await using (var seed = Open(dbName))
        {
            var folderId = SeedFolder(seed);
            var source = SeedProject(seed, folderId, "Källa");
            var target = SeedProject(seed, folderId, "Mål");
            await seed.SaveChangesAsync();

            var calc = SeedCalculation(seed, source.Id, "Kalkyl", isPrivate: false, statusId: null);
            await seed.SaveChangesAsync();

            SeedTaskWithResource(seed, calc.Id, cost: 250m, accountId: 0);
            await seed.SaveChangesAsync();

            targetProjectId = target.Id;
            sourceCalcId = calc.Id;
        }

        var bytes = await service.BuildCalculationPackageAsync(
            sourceCalcId, new AtacostCalculationExportRequest { Message = null },
            userId: Admin, departmentId: null, isViewer: false);

        Assert.NotNull(bytes);

        var result = await service.ImportCalculationPackageAsync(
            bytes!, targetProjectId, userId: Admin, departmentId: null, allowCrossDepartment: true);

        Assert.True(result.Success);
        var newCalcId = result.NewCalculationId;
        Assert.True(newCalcId > 0);
        Assert.NotEqual(sourceCalcId, newCalcId);

        await using var verify = Open(dbName);
        var imported = await verify.Calculations
            .Include(c => c.Tasks).ThenInclude(t => t.Resources)
            .FirstOrDefaultAsync(c => c.Id == newCalcId);

        Assert.NotNull(imported);
        Assert.Equal(targetProjectId, imported!.ProjectId);
        var task = Assert.Single(imported.Tasks);
        var resource = Assert.Single(task.Resources);
        Assert.Equal(250m, resource.GetMetadataSnapshot().Cost);
        Assert.True(imported.GetMetadataSnapshot().ImportInfo?.IsImportedCopy);
        Assert.Equal("Mål", imported.GetMetadataSnapshot().ImportInfo!.TargetProject);
    }

    [Fact]
    public async Task Private_calculation_cannot_be_exported_directly()
    {
        const string dbName = "Atacost_Private_Blocked";
        var factory = new FakeFactory(dbName);
        var service = new AtacostTransferService(factory, NullLogger<AtacostTransferService>.Instance);

        int privateCalcId;
        await using (var seed = Open(dbName))
        {
            var folderId = SeedFolder(seed);
            var project = SeedProject(seed, folderId, "P");
            await seed.SaveChangesAsync();

            var secret = SeedCalculation(seed, project.Id, "Hemlig", isPrivate: true, statusId: null);
            await seed.SaveChangesAsync();
            privateCalcId = secret.Id;
        }

        var bytes = await service.BuildCalculationPackageAsync(
            privateCalcId, new AtacostCalculationExportRequest(),
            userId: Admin, departmentId: null, isViewer: false);

        Assert.Null(bytes); // private calculations are never exported externally
    }

    [Fact]
    public async Task Inspect_returns_invalid_for_garbage_bytes()
    {
        var service = new AtacostTransferService(new FakeFactory("Atacost_Inspect"), NullLogger<AtacostTransferService>.Instance);
        var info = await service.InspectPackageAsync([1, 2, 3, 4]);
        Assert.False(info.IsValid);
    }
}

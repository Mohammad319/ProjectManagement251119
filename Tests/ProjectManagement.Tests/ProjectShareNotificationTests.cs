using Application.Interfaces;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Notifications;
using Domain.Entities.Project;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Factory;
using Persistence.Service.Project;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Base.Users;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;
using Xunit;

namespace ProjectManagement.Tests;

/// <summary>
/// Covers the notification side-effects of project sharing in <see cref="ProjectShareService"/>:
/// who gets notified, the structured content, change aggregation, department fan-out with dedup,
/// the system-Visare access cap, and removal notices. All rows use TenantId 0 so the global query
/// filter matches both seeded and service-created rows (mirrors AtacostTransferTests).
/// </summary>
public sealed class ProjectShareNotificationTests
{
    private const int Actor = 1;
    private const int DeptId = 7;
    private const string Edit = PMRolesConst.Tenant.Manger;   // "TenantUser"
    private const string View = PMRolesConst.Tenant.Viewer;   // "TenantViewer"

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

    private sealed class CapturingPublisher : INotificationPublisher
    {
        public List<int> Captured { get; } = [];
        public Task PublishUnreadChangedAsync(IEnumerable<int> userIds, CancellationToken ct = default)
        {
            Captured.AddRange(userIds);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeViewerProvider(params string[] viewerAuthIds) : IUserSystemRoleProvider
    {
        private readonly HashSet<string> _viewers = new(viewerAuthIds, StringComparer.Ordinal);
        public Task<IReadOnlySet<string>> GetViewerAuthIdsAsync(IEnumerable<string> externalAuthIds, CancellationToken ct = default)
            => Task.FromResult<IReadOnlySet<string>>(externalAuthIds.Where(_viewers.Contains).ToHashSet(StringComparer.Ordinal));
    }

    private static ShardingSingleDbContext Open(string dbName)
    {
        var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ShardingSingleDbContext(options) { TenantId = 0 };
    }

    private static Guid SeedProject(ShardingSingleDbContext db, string name, int calcCount)
    {
        var folder = new FolderEntity("Folder", "#112233", DeptId, Actor, 100) { Id = Guid.NewGuid() };
        db.Folders.Add(folder);

        var project = ProjectEntity.Create(
            new PostProjectDTO { Name = name, Code = name, FolderId = folder.Id }, folder.Id, Actor, 100);
        project.Id = Guid.NewGuid();
        db.Projects.Add(project);

        for (int i = 0; i < calcCount; i++)
        {
            var calc = new CalculationEntity { Id = 11 + i };
            calc.AssignToProject(project.Id);
            calc.AssignDepartment(DeptId);
            calc.Update(new CalculationPostDTO { Name = $"Calc {i}", Code = $"C{i}" });
            calc.InitializeVersionGroup();
            calc.CreatedBy = Actor;
            db.Calculations.Add(calc);
        }

        return project.Id;
    }

    private static void SeedUser(ShardingSingleDbContext db, int id, int? departmentId, string authId, string first, string last)
    {
        var user = UserEntity.Create(0, $"{authId}@x.test", $"user{id}", departmentId, first, last, authId);
        user.Id = id;
        db.User.Add(user);
    }

    private static void SeedDepartment(ShardingSingleDbContext db, int id, string name)
    {
        var dept = DepartmentEntity.Create(new DepartmentBase { Name = name });
        dept.Id = id;
        db.Department.Add(dept);
    }

    private static List<NotificationEntity> Notifications(string dbName)
    {
        using var db = Open(dbName);
        return db.Notifications.AsNoTracking().ToList();
    }

    [Fact]
    public async Task DirectShare_NotifiesRecipient_WithStructuredContent()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid projectId;
        using (var db = Open(dbName))
        {
            projectId = SeedProject(db, "Demo", calcCount: 3);
            SeedUser(db, Actor, null, "auth-1", "Anna", "Andersson");
            SeedUser(db, 101, null, "auth-101", "Erik", "Eriksson");
            await db.SaveChangesAsync();
        }

        var publisher = new CapturingPublisher();
        var service = new ProjectShareService(new FakeFactory(dbName), publisher);

        await service.UpsertAsync(projectId, new ProjectShareUpsertDTO
        {
            RecipientType = ProjectShareRecipientType.User,
            UserId = 101,
            Role = Edit,
            CalculationIds = [11, 12]
        }, Actor, null);

        var notif = Assert.Single(Notifications(dbName));
        Assert.Equal(NotificationType.ProjectSharedWithUser, notif.Type);
        Assert.Equal(101, notif.UserId);
        Assert.Equal("Demo", notif.ProjectName);
        Assert.Equal("Anna Andersson", notif.ActorName);
        Assert.Equal(Edit, notif.Role);
        Assert.Equal(2, notif.CalcCount);
        Assert.False(notif.CalcAllAvailable);
        Assert.Null(notif.ReadAt);
        Assert.Contains(101, publisher.Captured);
    }

    [Fact]
    public async Task ShareWithAllCalculations_SetsCalcAllAvailable()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid projectId;
        using (var db = Open(dbName))
        {
            projectId = SeedProject(db, "Demo", calcCount: 3);
            SeedUser(db, 101, null, "auth-101", "Erik", "Eriksson");
            await db.SaveChangesAsync();
        }

        var service = new ProjectShareService(new FakeFactory(dbName));
        await service.UpsertAsync(projectId, new ProjectShareUpsertDTO
        {
            RecipientType = ProjectShareRecipientType.User,
            UserId = 101,
            Role = Edit,
            CalculationIds = [11, 12, 13]
        }, Actor, null);

        var notif = Assert.Single(Notifications(dbName));
        Assert.True(notif.CalcAllAvailable);
        Assert.Equal(3, notif.CalcCount);
    }

    [Fact]
    public async Task SharingWithYourself_CreatesNoNotification()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid projectId;
        using (var db = Open(dbName))
        {
            projectId = SeedProject(db, "Demo", calcCount: 1);
            SeedUser(db, 101, null, "auth-101", "Erik", "Eriksson");
            await db.SaveChangesAsync();
        }

        var service = new ProjectShareService(new FakeFactory(dbName));
        await service.UpsertAsync(projectId, new ProjectShareUpsertDTO
        {
            RecipientType = ProjectShareRecipientType.User,
            UserId = 101,
            Role = Edit,
            CalculationIds = [11]
        }, userId: 101, departmentId: null);

        Assert.Empty(Notifications(dbName));
    }

    [Fact]
    public async Task MultipleChangesInOneUpdate_ProduceSingleAggregatedNotification()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid projectId;
        using (var db = Open(dbName))
        {
            projectId = SeedProject(db, "Demo", calcCount: 3);
            SeedUser(db, 101, null, "auth-101", "Erik", "Eriksson");
            await db.SaveChangesAsync();
        }

        var service = new ProjectShareService(new FakeFactory(dbName));

        var shareId = await service.UpsertAsync(projectId, new ProjectShareUpsertDTO
        {
            RecipientType = ProjectShareRecipientType.User,
            UserId = 101,
            Role = Edit,
            CalculationIds = [11]
        }, Actor, null);

        // Change role + validity + calculations all at once.
        await service.UpsertAsync(projectId, new ProjectShareUpsertDTO
        {
            Id = shareId,
            RecipientType = ProjectShareRecipientType.User,
            UserId = 101,
            Role = View,
            ValidUntil = DateTime.UtcNow.Date.AddDays(30),
            CalculationIds = [11, 12]
        }, Actor, null);

        var all = Notifications(dbName);
        Assert.Single(all, n => n.Type == NotificationType.ProjectAccessChanged);
        Assert.DoesNotContain(all, n => n.Type == NotificationType.ProjectShareValidityChanged);
        Assert.DoesNotContain(all, n => n.Type == NotificationType.ProjectSharedCalculationsChanged);
    }

    [Fact]
    public async Task DepartmentShare_SkipsUsersWithEqualOrStrongerDirectShare()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid projectId;
        using (var db = Open(dbName))
        {
            projectId = SeedProject(db, "Demo", calcCount: 2);
            SeedDepartment(db, DeptId, "Produktion");
            SeedUser(db, 201, DeptId, "auth-201", "Direct", "User");
            SeedUser(db, 202, DeptId, "auth-202", "Dept", "User");

            // 201 already has a direct edit share → must not be re-notified by the dept share.
            var direct = ProjectShareEntity.ForUser(projectId, 201, Edit);
            db.ProjectShare.Add(direct);
            await db.SaveChangesAsync();
        }

        var service = new ProjectShareService(new FakeFactory(dbName));
        await service.UpsertAsync(projectId, new ProjectShareUpsertDTO
        {
            RecipientType = ProjectShareRecipientType.Department,
            DepartmentId = DeptId,
            Role = Edit,
            CalculationIds = [11]
        }, Actor, null);

        var deptNotifs = Notifications(dbName)
            .Where(n => n.Type == NotificationType.ProjectSharedWithDepartment)
            .ToList();

        var notif = Assert.Single(deptNotifs);
        Assert.Equal(202, notif.UserId);
        Assert.Equal("Produktion", notif.DepartmentName);
    }

    [Fact]
    public async Task DepartmentShareAsEdit_CapsSystemViewerToView()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid projectId;
        using (var db = Open(dbName))
        {
            projectId = SeedProject(db, "Demo", calcCount: 1);
            SeedDepartment(db, DeptId, "Produktion");
            SeedUser(db, 301, DeptId, "auth-301", "Visare", "Person");
            SeedUser(db, 302, DeptId, "auth-302", "Normal", "Person");
            await db.SaveChangesAsync();
        }

        // 301 is a system Visare, 302 is not.
        var service = new ProjectShareService(new FakeFactory(dbName), publisher: null, roleProvider: new FakeViewerProvider("auth-301"));
        await service.UpsertAsync(projectId, new ProjectShareUpsertDTO
        {
            RecipientType = ProjectShareRecipientType.Department,
            DepartmentId = DeptId,
            Role = Edit,
            CalculationIds = [11]
        }, Actor, null);

        var byUser = Notifications(dbName).ToDictionary(n => n.UserId);
        Assert.Equal(View, byUser[301].Role);   // capped
        Assert.Equal(Edit, byUser[302].Role);   // unchanged
    }

    [Fact]
    public async Task DeletingShare_NotifiesAffectedUser()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid projectId;
        int shareId;
        using (var db = Open(dbName))
        {
            projectId = SeedProject(db, "Demo", calcCount: 1);
            SeedUser(db, 101, null, "auth-101", "Erik", "Eriksson");
            await db.SaveChangesAsync();
        }

        var service = new ProjectShareService(new FakeFactory(dbName));
        shareId = await service.UpsertAsync(projectId, new ProjectShareUpsertDTO
        {
            RecipientType = ProjectShareRecipientType.User,
            UserId = 101,
            Role = Edit,
            CalculationIds = [11]
        }, Actor, null);

        await service.DeleteAsync(shareId, Actor, null);

        var removed = Assert.Single(Notifications(dbName), n => n.Type == NotificationType.ProjectAccessRemoved);
        Assert.Equal(101, removed.UserId);
        Assert.Equal("Demo", removed.ProjectName);
    }
}

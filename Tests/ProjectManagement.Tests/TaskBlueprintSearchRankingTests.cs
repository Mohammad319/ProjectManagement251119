using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.Helper.Text;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure;
using TaskResourceBlueprints.Services.ProjectTask;
using Xunit;

namespace ProjectManagement.Tests;

public class TaskBlueprintSearchRankingTests
{
    [Fact]
    public void SearchRanking_PutsPrefixMatchAboveHighUsageWeakMatch()
    {
        var search = BuildSearch("sch");
        var schakt = Candidate(1, "Schakt f\u00f6r ledning", "CBB.1", 2);
        var weakHighUsage = Candidate(2, "M\u00e5lning av v\u00e4gg", "HUS.1", 900);

        Assert.True(Score(search, schakt) > Score(search, weakHighUsage));
    }

    [Fact]
    public void SearchRanking_PutsExactCodeMatchFirst()
    {
        var search = BuildSearch("CBB.311");
        var exact = Candidate(1, "Schakt f\u00f6r ledning", "CBB.311", 0);
        var textMatch = Candidate(2, "Schakt CBB arbete", "CBB.100", 500);

        Assert.True(Score(search, exact) > Score(search, textMatch));
    }

    [Fact]
    public void SearchRanking_RewardsFullTokenCoverage()
    {
        var search = BuildSearch("schakt ledning");
        var full = Candidate(1, "Schakt f\u00f6r ledning", "CBB.1", 1);
        var partial = Candidate(2, "Schakt f\u00f6r grund", "CBB.2", 100);

        Assert.True(Score(search, full) > Score(search, partial));
    }

    [Fact]
    public void SearchRanking_RewardsMatchingBigram()
    {
        var filter = new ProjectTaskFilterDto
        {
            SearchTokens = ["schakt", "ledning", "schakt_ledning", "ledning_schakt"]
        };
        var search = BuildSearch(filter);
        var withBigram = Candidate(1, "Schakt ledning", "CBB.1", 0);
        var withoutBigram = Candidate(2, "Schakt runt befintlig ledning", "CBB.2", 0);

        Assert.True(Score(search, withBigram) > Score(search, withoutBigram));
    }

    [Fact]
    public void SearchRanking_UsesParentContextForChildTasks()
    {
        var search = BuildSearch("jordschakt kabel");
        var childWithParentContext = Candidate(
            1,
            "Gr\u00f6na ytor, markklass 2, minsta fyllningsh\u00f6jd 0,5 m",
            string.Empty,
            0,
            SwedishTaskTextNormalizer.Normalize("CBB.32 Jordschakt f\u00f6r el- och telekabel Gr\u00f6na ytor markklass"));
        var unrelated = Candidate(
            2,
            "Gr\u00f6na ytor, markklass 2, minsta fyllningsh\u00f6jd 0,5 m",
            string.Empty,
            100,
            SwedishTaskTextNormalizer.NormalizeTask("Gr\u00f6na ytor, markklass 2, minsta fyllningsh\u00f6jd 0,5 m", null, null));

        Assert.True(Score(search, childWithParentContext) > Score(search, unrelated));
    }

    [Fact]
    public void SearchRanking_RewardsResourceTextMatch()
    {
        var search = BuildSearch("betong");
        var resourceMatch = Candidate(1, "Gjutning av fundament", "EBC.1", 0, resourceText: "Betong C25/30 m3");
        var unrelated = Candidate(2, "Schakt for ledning", "CBB.1", 250);

        Assert.True(Score(search, resourceMatch) > Score(search, unrelated));
    }

    [Fact]
    public async Task SearchService_ReturnsTaskWhenOnlyResourceNameMatchesTypo()
    {
        var options = new DbContextOptionsBuilder<TaskResourceBlueprintsContext>()
            .UseInMemoryDatabase($"blueprint-search-{Guid.NewGuid():N}")
            .Options;
        await using (var db = new TaskResourceBlueprintsContext(options))
        {
            var concreteTask = new TaskDefinition
            {
                Id = 1,
                Name = "Gjutning av fundament",
                Code = "EBC.1",
                Status = TaskStatusEnum.Ready,
                UsageCount = 0,
                RowVersion = [1]
            };
            concreteTask.RefreshNormalizedTextSv();

            var unrelatedTask = new TaskDefinition
            {
                Id = 2,
                Name = "Schakt for ledning",
                Code = "CBB.1",
                Status = TaskStatusEnum.Ready,
                UsageCount = 500,
                RowVersion = [1]
            };
            unrelatedTask.RefreshNormalizedTextSv();

            var concreteResource = new ResourceDefinition
            {
                Id = 10,
                Name = "Betong C25/30",
                Unit = "m3",
                IsActive = true,
                IsVisible = true
            };
            var concreteLink = new TaskDefinitionResourceLink
            {
                TaskDefinitionId = concreteTask.Id,
                Task = concreteTask,
                ResourceDefinitionId = 10,
                Resource = concreteResource,
                Quantity = 1,
                RowVersion = [1]
            };
            concreteTask.ResourceLinks.Add(concreteLink);

            db.Tasks.AddRange(concreteTask, unrelatedTask);
            db.Resources.Add(concreteResource);
            db.TaskDefinitionResourceLinks.Add(concreteLink);
            await db.SaveChangesAsync();
        }

        var service = new ProjectTaskService(new TestBlueprintContextFactory(options));
        var result = await service.GetTasksForUserDtoAsync(new ProjectTaskFilterDto
        {
            NameOrCode = "betogn",
            ResourcesOnly = true,
            Take = 10
        }, tenantid: 1, CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.Equal(1, result[0].Id);
        Assert.Contains("Betong", result[0].ResourceSearchText);
    }

    private static object BuildSearch(string rawQuery)
    {
        var filter = new ProjectTaskFilterDto { NameOrCode = rawQuery };
        return BuildSearch(filter);
    }

    private static object BuildSearch(ProjectTaskFilterDto filter)
    {
        var method = typeof(ProjectTaskService).GetMethod(
            "BuildTaskSearchContext",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var tokens = filter.SearchTokens
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();

        return method.Invoke(null, [filter, tokens])!;
    }

    private static object Candidate(int id, string name, string code, int usageCount, string? normalizedText = null, string resourceText = "")
    {
        var type = typeof(ProjectTaskService).GetNestedType(
            "TaskSearchCandidate",
            BindingFlags.NonPublic);
        Assert.NotNull(type);

        return Activator.CreateInstance(type, [
            id,
            name,
            code,
            normalizedText ?? SwedishTaskTextNormalizer.NormalizeTask(name, code, null),
            usageCount,
            resourceText
        ])!;
    }

    private static double Score(object search, object candidate)
    {
        var method = typeof(ProjectTaskService).GetMethod(
            "CalculateTaskSearchScore",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return (double)method.Invoke(null, [search, candidate])!;
    }

    private sealed class TestBlueprintContextFactory(DbContextOptions<TaskResourceBlueprintsContext> options)
        : IDbContextFactory<TaskResourceBlueprintsContext>
    {
        public TaskResourceBlueprintsContext CreateDbContext()
            => new(options);
    }
}

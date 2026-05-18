using System.Reflection;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.Helper.Text;
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

    private static object Candidate(int id, string name, string code, int usageCount, string? normalizedText = null)
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
            usageCount
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
}

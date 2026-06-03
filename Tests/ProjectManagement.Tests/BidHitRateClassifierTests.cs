using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Helper;
using Xunit;

namespace ProjectManagement.Tests;

public class BidHitRateClassifierTests
{
    [Fact]
    public void ClassifyProject_WonStatus_IsWon()
    {
        var project = CreateProject(submitted: true, won: true);

        Assert.Equal(BidHitRateResult.Won, BidHitRateClassifier.ClassifyProject(project));
    }

    [Fact]
    public void ClassifyProject_LostStatus_IsLost()
    {
        var project = CreateProject(submitted: true, lost: true);

        Assert.Equal(BidHitRateResult.Lost, BidHitRateClassifier.ClassifyProject(project));
    }

    [Fact]
    public void ClassifyProject_SubmittedWithoutDecision_IsUndecided()
    {
        var project = CreateProject(submitted: true);

        Assert.Equal(BidHitRateResult.Undecided, BidHitRateClassifier.ClassifyProject(project));
    }

    [Fact]
    public void ClassifyProject_NotSubmitted_IsExcluded()
    {
        var project = CreateProject();

        Assert.Equal(BidHitRateResult.Excluded, BidHitRateClassifier.ClassifyProject(project));
    }

    [Fact]
    public void ClassifyProject_NullProject_IsExcluded()
    {
        Assert.Equal(BidHitRateResult.Excluded, BidHitRateClassifier.ClassifyProject(null));
    }

    private static ListProjectDTO CreateProject(
        bool submitted = false,
        bool won = false,
        bool lost = false)
    {
        return new ListProjectDTO
        {
            CountsAsSubmittedBid = submitted,
            CountsAsWonBid = won,
            CountsAsLostBid = lost
        };
    }
}

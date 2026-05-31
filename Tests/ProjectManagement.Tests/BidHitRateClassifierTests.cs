using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Helper;
using Xunit;

namespace ProjectManagement.Tests;

public class BidHitRateClassifierTests
{
    [Fact]
    public void ClassifyProject_WonMainBid_IsNotOverriddenByLostAdditionalWork()
    {
        var calculations = new[]
        {
            CreateCalculation(1, BidRole.MainBid, submitted: true, won: true),
            CreateCalculation(2, BidRole.AdditionalWork, submitted: true, lost: true)
        };

        Assert.Equal(BidHitRateResult.Won, BidHitRateClassifier.ClassifyProject(calculations));
    }

    [Fact]
    public void ClassifyProject_UsesOnlyCurrentVersionPerCalculationFamily()
    {
        var familyId = Guid.NewGuid();
        var calculations = new[]
        {
            CreateCalculation(1, BidRole.MainBid, submitted: true, lost: true, familyId: familyId, versionNumber: 1, isCurrent: false),
            CreateCalculation(2, BidRole.MainBid, submitted: true, won: true, familyId: familyId, versionNumber: 2, isCurrent: true)
        };

        Assert.Equal(BidHitRateResult.Won, BidHitRateClassifier.ClassifyProject(calculations));
    }

    [Fact]
    public void ClassifyProject_SubmittedWithoutDecision_IsUndecided()
    {
        var calculations = new[]
        {
            CreateCalculation(1, BidRole.MainBid, submitted: true)
        };

        Assert.Equal(BidHitRateResult.Undecided, BidHitRateClassifier.ClassifyProject(calculations));
    }

    [Fact]
    public void ClassifyProject_WithoutSubmittedMainBid_IsExcluded()
    {
        var calculations = new[]
        {
            CreateCalculation(1, BidRole.Option, submitted: true, won: true),
            CreateCalculation(2, BidRole.InternalObject, submitted: true, lost: true)
        };

        Assert.Equal(BidHitRateResult.Excluded, BidHitRateClassifier.ClassifyProject(calculations));
    }

    private static ListCalculationDTO CreateCalculation(
        int id,
        BidRole bidRole,
        bool submitted = false,
        bool won = false,
        bool lost = false,
        Guid? familyId = null,
        int versionNumber = 1,
        bool isCurrent = true)
    {
        return new ListCalculationDTO
        {
            Id = id,
            BidRole = bidRole,
            CountsAsSubmittedBid = submitted,
            CountsAsWonBid = won,
            CountsAsLostBid = lost,
            VersionGroupId = familyId ?? Guid.NewGuid(),
            VersionNumber = versionNumber,
            IsCurrentVersion = isCurrent,
            CreatedAt = new DateTime(2026, 1, id)
        };
    }
}

using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Policies;
using Xunit;

namespace ProjectManagement.Tests;

public class CalculationStatusPolicyTests
{
    private static readonly CalculationStatusPolicyStatus ApprovedLocked = new(
        Id: 1,
        IsApprovalStatus: true,
        LocksCalculation: true,
        AllowsProductionCalculation: false,
        CountsAsSubmittedBid: false,
        CountsAsWonBid: false,
        CountsAsLostBid: false);

    private static readonly CalculationStatusPolicyStatus Submitted = new(
        Id: 2,
        IsApprovalStatus: false,
        LocksCalculation: false,
        AllowsProductionCalculation: false,
        CountsAsSubmittedBid: true,
        CountsAsWonBid: false,
        CountsAsLostBid: false);

    private static readonly CalculationStatusPolicyStatus Won = new(
        Id: 3,
        IsApprovalStatus: true,
        LocksCalculation: true,
        AllowsProductionCalculation: true,
        CountsAsSubmittedBid: true,
        CountsAsWonBid: true,
        CountsAsLostBid: false);

    private static readonly CalculationStatusPolicyStatus Lost = new(
        Id: 4,
        IsApprovalStatus: false,
        LocksCalculation: false,
        AllowsProductionCalculation: false,
        CountsAsSubmittedBid: true,
        CountsAsWonBid: false,
        CountsAsLostBid: true);

    private static readonly CalculationStatusPolicyStatus InProgress = new(
        Id: 5,
        IsApprovalStatus: false,
        LocksCalculation: false,
        AllowsProductionCalculation: false,
        CountsAsSubmittedBid: false,
        CountsAsWonBid: false,
        CountsAsLostBid: false);

    [Fact]
    public void LockedApproved_AllowsSubmittedAndWon()
    {
        Assert.True(CalculationStatusPolicy.IsAllowedLockedStatus(ApprovedLocked, Submitted));
        Assert.True(CalculationStatusPolicy.IsAllowedLockedStatus(ApprovedLocked, Won));
    }

    [Fact]
    public void LockedSubmitted_AllowsWonAndLost()
    {
        Assert.True(CalculationStatusPolicy.IsAllowedLockedStatus(Submitted, Won));
        Assert.True(CalculationStatusPolicy.IsAllowedLockedStatus(Submitted, Lost));
    }

    [Fact]
    public void LockedWon_BlocksBackwardTransitions()
    {
        Assert.False(CalculationStatusPolicy.IsAllowedLockedStatus(Won, Submitted));
        Assert.False(CalculationStatusPolicy.IsAllowedLockedStatus(Won, InProgress));
    }

    [Fact]
    public void LockedCalculation_DoesNotAllowReopenToInProgress()
    {
        Assert.False(CalculationStatusPolicy.IsAllowedLockedStatus(ApprovedLocked, InProgress));
    }

    [Fact]
    public void CopyActions_RequireLockedWonStatus()
    {
        Assert.True(CalculationStatusPolicy.CanCreateContractCalculation(CalculationVersionType.Tender, isLocked: true, Won));
        Assert.True(CalculationStatusPolicy.CanCreateProductionCalculation(CalculationVersionType.Contract, isLocked: true, Won));

        Assert.False(CalculationStatusPolicy.CanCreateContractCalculation(CalculationVersionType.Tender, isLocked: false, Won));
        Assert.False(CalculationStatusPolicy.CanCreateProductionCalculation(CalculationVersionType.Tender, isLocked: true, Submitted));
    }
}

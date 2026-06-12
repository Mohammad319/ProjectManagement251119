using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Shared.Policies;

public readonly record struct CalculationStatusPolicyStatus(
    int Id,
    bool IsApprovalStatus,
    bool LocksCalculation,
    bool AllowsProductionCalculation,
    bool CountsAsSubmittedBid,
    bool CountsAsWonBid,
    bool CountsAsLostBid);

public static class CalculationStatusPolicy
{
    public static bool IsAllowedLockedStatus(
        CalculationStatusPolicyStatus? currentStatus,
        CalculationStatusPolicyStatus requestedStatus)
    {
        if (currentStatus.HasValue && requestedStatus.Id == currentStatus.Value.Id)
            return true;

        if (currentStatus?.CountsAsWonBid == true)
            return false;

        if (currentStatus?.CountsAsSubmittedBid == true)
            return requestedStatus.CountsAsWonBid || requestedStatus.CountsAsLostBid;

        return requestedStatus.CountsAsSubmittedBid || requestedStatus.CountsAsWonBid;
    }

    public static bool HasAllowedLockedStatusChange(
        int? originalStatusId,
        int? requestedStatusId,
        CalculationStatusPolicyStatus? currentStatus,
        CalculationStatusPolicyStatus? requestedStatus)
    {
        return requestedStatusId.HasValue &&
            requestedStatusId != originalStatusId &&
            requestedStatus.HasValue &&
            IsAllowedLockedStatus(currentStatus, requestedStatus.Value);
    }

    public static bool CanCreateContractCalculation(
        CalculationVersionType calculationType,
        bool isLocked,
        CalculationStatusPolicyStatus? status)
    {
        return calculationType == CalculationVersionType.Tender &&
            isLocked &&
            status?.CountsAsWonBid == true;
    }

    public static bool CanCreateProductionCalculation(
        CalculationVersionType calculationType,
        bool isLocked,
        CalculationStatusPolicyStatus? status)
    {
        return (calculationType == CalculationVersionType.Tender ||
                calculationType == CalculationVersionType.Contract) &&
            isLocked &&
            status?.CountsAsWonBid == true;
    }
}

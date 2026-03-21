using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Shared.DTO.Offer;

namespace ProjectManagement.Client.Pages.Calculation.Table;

public partial class CalculationOfferBadge
{
    [Inject] private ICalculationTableCoordinator TableCoordinator { get; set; } = default!;

    [Parameter] public ListOfferDTO? Offer { get; set; }

    private string BadgeText => CalculationToolbarTextHelper.BuildOfferBadgeText(Offer);
}

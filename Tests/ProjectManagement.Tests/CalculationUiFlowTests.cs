using System.Reflection;
using ProjectManagement.Client.Pages.Calculation.Table;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.DTO.Offer;
using ProjectManagement.Shared.Enums;
using Xunit;

namespace ProjectManagement.Tests;

public class CalculationUiFlowTests
{
    [Fact]
    public void RequestGridRefresh_FlatList_MarksFlatListDirty_AndRaisesEvent()
    {
        var folderState = new FolderState();
        var calc = new CalculationMVVM();
        folderState.SetCalculation(calc);

        var service = new CalculationService(null!, null!, folderState, null!, new CalculationInteractionState());

        int refreshCalls = 0;
        calc.OnChangeInCalculation += () => refreshCalls++;

        calc.FlatListDirty = false;
        service.RequestGridRefresh(CalculationGridRefreshKind.FlatList);

        Assert.True(calc.FlatListDirty);
        Assert.Equal(1, refreshCalls);
    }

    [Fact]
    public void RequestGridRefresh_Structure_MarksStructureDirty()
    {
        var folderState = new FolderState();
        var calc = new CalculationMVVM();
        folderState.SetCalculation(calc);

        var service = new CalculationService(null!, null!, folderState, null!, new CalculationInteractionState());

        service.RequestGridRefresh(CalculationGridRefreshKind.Structure);

        var structureDirty = typeof(CalculationMVVM)
            .GetProperty("StructureFlatListDirty", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(calc);

        Assert.True(calc.FlatListDirty);
        Assert.True((bool)structureDirty!);
    }

    [Fact]
    public void Coordinator_UnselectAll_ClearsSelection_WithoutGridRefresh()
    {
        var folderState = new FolderState();
        var calc = new CalculationMVVM();
        folderState.SetCalculation(calc);

        var interactionState = new CalculationInteractionState();
        interactionState.SetModifierKey("Control");
        interactionState.HandleItemSelected(42, 5m, CalculationItemType.task);

        var service = new CalculationService(null!, null!, folderState, null!, interactionState);
        var coordinator = new CalculationTableCoordinator(folderState, null!, null!, null!, interactionState, service, null!);

        int refreshCalls = 0;
        calc.OnChangeInCalculation += () => refreshCalls++;

        coordinator.UnselectAll();

        Assert.Empty(interactionState.SelectedItems);
        Assert.Equal(0, refreshCalls);
    }

    [Fact]
    public void BuildOfferBadgeText_UsesCategoryPath_Once()
    {
        var text = CalculationToolbarTextHelper.BuildOfferBadgeText(new ListOfferDTO
        {
            Category = "Cat",
            SubCategory = "Sub",
            Organisation = "Org"
        });

        Assert.Contains("Cat > Sub", text);
        Assert.DoesNotContain("Sub>Sub", text);
        Assert.Contains("Org", text);
    }
}

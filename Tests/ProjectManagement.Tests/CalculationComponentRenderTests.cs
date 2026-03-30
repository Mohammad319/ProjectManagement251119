using AngleSharp.Dom;
using BlazorMHD.UI.Components.Feedback.Splitter;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Pages.Calculation.Table;
using ProjectManagement.Client.Pages.Calculation.Table.SectionsList;
using ProjectManagement.Client.Pages.Calculation.Table.SectionsList.Rows;
using ProjectManagement.Client.Pages.Project.Storage;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Client.Services.Calculation.CalculationItems;
using ProjectManagement.Client.Services.MHDBlazor;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Client.Shared.Repositories.ResourceType;
using ProjectManagement.Client.Shared.ViewModel;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Constants;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Offer;
using ProjectManagement.Shared.DTO.Calculation.Template;
using ProjectManagement.Shared.DTO.ResourceType;
using ProjectManagement.Shared.Enums;
using System.Runtime.CompilerServices;
using Xunit;
using ClientResourceService = ProjectManagement.Client.Services.Calculation.CalculationItems.ResourceService;

namespace ProjectManagement.Tests;

public class CalculationComponentRenderTests : BunitContext
{
    [Fact]
    public void CalculationHierarchyCell_RendersToggle_AndInvokesCallback()
    {
        int toggleCount = 0;

        var cut = Render<CalculationHierarchyCell>(parameters => parameters
            .Add(x => x.IndentPx, 24)
            .Add(x => x.CanToggle, true)
            .Add(x => x.IsExpanded, true)
            .Add(x => x.OnToggle, () => toggleCount++));

        var container = cut.Find("td > div");
        Assert.Contains("padding-left:24px", container.GetAttribute("style"));

        cut.Find("span[role='button']").Click();

        Assert.Equal(1, toggleCount);
    }

    [Fact]
    public void CalculationHierarchyCell_RendersPlaceholder_WhenRequested()
    {
        var cut = Render<CalculationHierarchyCell>(parameters => parameters
            .Add(x => x.ShowPlaceholder, true));

        var placeholder = cut.Find("td span");
        Assert.DoesNotContain("role", placeholder.Attributes.Select(x => x.Name));
    }

    [Fact]
    public void CalculationOfferBadge_RendersOfferText_AndClearsSelection()
    {
        var coordinator = new FakeCalculationTableCoordinator();
        Services.AddSingleton<ICalculationTableCoordinator>(coordinator);

        var cut = Render<CalculationOfferBadge>(parameters => parameters
            .Add(x => x.Offer, new ListOfferDTO
            {
                Category = "Cat",
                SubCategory = "Sub",
                Organisation = "Org"
            }));

        var badgeText = cut.Find("span").TextContent;

        Assert.Contains("Cat", badgeText);
        Assert.Contains("Sub", badgeText);
        Assert.Contains("Org", badgeText);

        cut.Find("span[role='button']").Click();

        Assert.Equal(1, coordinator.ClearOfferSelectionCalls);
    }

    [Fact]
    public void CalculationDisplayOptions_TogglesFlags_AndNotifiesCoordinator()
    {
        var coordinator = new FakeCalculationTableCoordinator();
        var calc = new CalculationMVVM
        {
            ShowTasks = true,
            ShowResources = true,
            OnlyActive = false
        };
        var folderState = CreateFolderState(calc);
        var calcService = CreateCalculationService();
        calcService.ShowComments = true;

        RegisterCalculationComponentServices(coordinator, calcService, folderState, new CalculationInteractionState());

        var cut = Render<CalculationDisplayOptions>();

        cut.Find("div.relative > span").Click();

        var checkboxes = cut.FindAll("input[type='checkbox']");
        Assert.Equal(4, checkboxes.Count);

        checkboxes[0].Change(false);
        Assert.False(calc.ShowTasks);
        Assert.Equal(1, coordinator.NotifyStructureRefreshCalls);

        checkboxes = cut.FindAll("input[type='checkbox']");
        checkboxes[1].Change(false);
        Assert.False(calc.ShowResources);
        Assert.Equal(2, coordinator.NotifyStructureRefreshCalls);

        checkboxes = cut.FindAll("input[type='checkbox']");
        checkboxes[2].Change(false);
        Assert.False(calcService.ShowComments);
        Assert.Equal(2, coordinator.NotifyStructureRefreshCalls);

        checkboxes = cut.FindAll("input[type='checkbox']");
        checkboxes[3].Change(true);
        Assert.True(calc.OnlyActive);
        Assert.Equal(3, coordinator.NotifyStructureRefreshCalls);
    }

    [Fact]
    public void CalculationToolbar_ClickingNewTask_UsesCoordinator()
    {
        AddAuthorization().SetNotAuthorized();

        var coordinator = new FakeCalculationTableCoordinator();
        var folderState = CreateFolderState(new CalculationMVVM());
        var calcService = CreateCalculationService();

        RegisterCalculationComponentServices(coordinator, calcService, folderState, new CalculationInteractionState());

        var cut = Render<CalculationToolbar>();

        cut.Find("#tit > span").Click();

        Assert.Equal(1, coordinator.ShowTaskFormCalls);
        Assert.NotNull(coordinator.LastTaskFormModel);
    }

    [Fact]
    public void CalculationToolbar_ReactsToSelection_AndShowsAuthorizedActions()
    {
        var auth = AddAuthorization();
        auth.SetAuthorized("tester");
        auth.SetRoles(
            PMRolesConst.Tenant.Admin,
            PMRolesConst.Tenant.SuperManger,
            PMRolesConst.Tenant.Manger);

        var coordinator = new FakeCalculationTableCoordinator { CanPasteResult = true };
        var interactionState = new CalculationInteractionState();
        var folderState = CreateFolderState(new CalculationMVVM { Tap1 = true });
        var calcService = CreateCalculationService();
        calcService.Offer = new ListOfferDTO
        {
            Category = "Cat",
            SubCategory = "Sub",
            Organisation = "Org"
        };

        RegisterCalculationComponentServices(coordinator, calcService, folderState, interactionState);

        var cut = Render<CalculationToolbar>();

        Assert.Empty(FindSpansByTitle(cut, ResourceLoc.unselectAll));
        Assert.Single(cut.FindComponents<CalculationOfferBadge>());
        Assert.Single(FindSpansByTitle(cut, ResourceApp.paste));

        interactionState.SetModifierKey("Control");
        interactionState.HandleItemSelected(15, 1, CalculationItemType.task);

        cut.WaitForAssertion(() => Assert.Single(FindSpansByTitle(cut, ResourceLoc.unselectAll)));

        FindSpansByTitle(cut, ResourceLoc.unselectAll).Single().Click();
        Assert.Equal(1, coordinator.UnselectAllCalls);

        FindSpansByTitle(cut, ResourceApp.paste).Single().Click();
        cut.WaitForAssertion(() => Assert.Equal(1, coordinator.PasteCalls));
    }

    [Fact]
    public void TaskRowComponent_ClickWithModifier_SelectsTask_AndAppliesSelectedStyle()
    {
        var interactionState = new CalculationInteractionState();
        var calcService = CreateCalculationService();
        RegisterRowComponentServices(interactionState, calcService);

        var task = new TaskListMVVM
        {
            Id = 42,
            Resources = [],
            Tasks = []
        };
        task.Metadata = new()
        {
            IsActive = true,
            Quantity = 5
        };

        var cut = Render<TaskRowComponent>(parameters => parameters
            .Add(x => x.Task, task)
            .Add(x => x.ActiveParent, true)
            .Add(x => x.Color, "#123456")
            .Add(x => x.Left, 10)
            .Add(x => x.Colmuns, CreateTestColumns()));

        Assert.Contains("#123456", cut.Find("tr").GetAttribute("style"));

        interactionState.SetModifierKey("Control");
        cut.Find("tr").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("background-color:#114356", cut.Find("tr").GetAttribute("style")));
    }

    [Fact]
    public void TaskRowComponent_NotesFollowCommentsVisibility()
    {
        var interactionState = new CalculationInteractionState();
        var calcService = CreateCalculationService();
        calcService.ShowComments = true;
        RegisterRowComponentServices(interactionState, calcService);

        var task = new TaskListMVVM
        {
            Id = 7,
            Resources = [],
            Tasks = []
        };
        task.Metadata = new()
        {
            IsActive = true,
            UpperNote = ["Note A", "Note B"]
        };

        var cut = Render<TaskRowComponent>(parameters => parameters
            .Add(x => x.Task, task)
            .Add(x => x.ActiveParent, true)
            .Add(x => x.Color, "#abcdef")
            .Add(x => x.Left, 0)
            .Add(x => x.Colmuns, CreateTestColumns()));

        Assert.Equal(3, cut.FindAll("tr").Count);
        Assert.Contains("Note A", cut.Markup);
        Assert.Contains("Note B", cut.Markup);

        calcService.ShowComments = false;

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("tr"));
            Assert.DoesNotContain("Note A", cut.Markup);
        });
    }

    [Fact]
    public void ResourceRowComponent_UsesInactiveBranchStyle_AndCanBecomeSelected()
    {
        var interactionState = new CalculationInteractionState();
        var calcService = CreateCalculationService();
        RegisterRowComponentServices(interactionState, calcService, CreateResourceService());

        var resource = new ResourceListMVVM
        {
            Id = 12,
            TaskId = 3,
            Active = true
        };
        resource.Data = new()
        {
            Quantity = 2,
            UpperNote = []
        };

        var cut = Render<ResourceRowComponent>(parameters => parameters
            .Add(x => x.Resource, resource)
            .Add(x => x.TaskBranchActive, false)
            .Add(x => x.Color, "#eeeeee")
            .Add(x => x.Left, 20)
            .Add(x => x.Colmuns, CreateTestColumns()));

        Assert.Contains("color:rgb(49 48 45 / 55%)", cut.Find("tr").GetAttribute("style"));

        interactionState.SetModifierKey("Control");
        cut.Find("tr").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("background-color:#114356", cut.Find("tr").GetAttribute("style")));
    }

    [Fact]
    public void ResourceRowComponent_NotesFollowCommentsVisibility()
    {
        var interactionState = new CalculationInteractionState();
        var calcService = CreateCalculationService();
        calcService.ShowComments = true;
        RegisterRowComponentServices(interactionState, calcService, CreateResourceService());

        var resource = new ResourceListMVVM
        {
            Id = 9,
            TaskId = 2,
            Active = true
        };
        resource.Data = new()
        {
            UpperNote = ["Res Note"]
        };

        var cut = Render<ResourceRowComponent>(parameters => parameters
            .Add(x => x.Resource, resource)
            .Add(x => x.TaskBranchActive, true)
            .Add(x => x.Color, "#ffffff")
            .Add(x => x.Left, 0)
            .Add(x => x.Colmuns, CreateTestColumns()));

        Assert.Equal(2, cut.FindAll("tr").Count);
        Assert.Contains("Res Note", cut.Markup);

        calcService.ShowComments = false;

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("tr"));
            Assert.DoesNotContain("Res Note", cut.Markup);
        });
    }

    [Fact]
    public void CalcDataGrid_RendersVisibleHeaders_AndBindsRowActions()
    {
        JSInterop.SetupVoid("initializeResizableColumns", _ => true);

        var calc = CreateGridCalculation();
        var interactionState = new CalculationInteractionState();
        var folderState = CreateFolderState(calc);
        var templateRepository = new FakeTemplateRepository();
        var calcService = CreateGridCalculationService(folderState, interactionState, templateRepository);
        var taskService = CreateTaskService();
        var resourceService = CreateResourceService();

        RegisterGridComponentServices(calcService, interactionState, folderState, templateRepository, taskService, resourceService);

        var cut = Render<CalcDataGrid>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(3, cut.FindAll("thead th").Count);
            Assert.Contains("name", cut.Markup);
            Assert.Contains("status", cut.Markup);
            Assert.Equal("true", cut.Find("#h1").GetAttribute("data-pm-frozen"));
            Assert.Equal("true", cut.Find("#h2").GetAttribute("data-pm-frozen"));
            Assert.Equal("false", cut.Find("#h3").GetAttribute("data-pm-frozen"));
            Assert.Single(cut.FindComponents<TaskRowComponent>());
            Assert.Single(cut.FindComponents<ResourceRowComponent>());
        });

        Assert.NotNull(calc.Tasks[0].Ui.ContextClick);
        Assert.NotNull(calc.Tasks[0].Resources[0].Ui.ContextClick);
        Assert.NotNull(calc.Tasks[0].Resources[0].Ui.OfferClick);
    }

    [Fact]
    public void CalcDataGrid_AlwaysFreezes_AccountAndNameColumns()
    {
        JSInterop.SetupVoid("initializeResizableColumns", _ => true);

        var calc = CreateGridCalculationWithAccountAndNameColumns();
        var interactionState = new CalculationInteractionState();
        var folderState = CreateFolderState(calc);
        var templateRepository = new FakeTemplateRepository();
        var calcService = CreateGridCalculationService(folderState, interactionState, templateRepository);

        RegisterGridComponentServices(
            calcService,
            interactionState,
            folderState,
            templateRepository,
            CreateTaskService(),
            CreateResourceService());

        var cut = Render<CalcDataGrid>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("true", cut.Find("#h2").GetAttribute("data-pm-frozen"));
            Assert.Equal("true", cut.Find("#h3").GetAttribute("data-pm-frozen"));
            Assert.Equal("false", cut.Find("#h4").GetAttribute("data-pm-frozen"));
        });
    }

    [Fact]
    public async Task CalcDataGrid_SaveTemplateBlazor_UpdatesVisibleColumnWidth()
    {
        JSInterop.SetupVoid("initializeResizableColumns", _ => true);

        var calc = CreateGridCalculation();
        calc.TemplateId = 25;
        var interactionState = new CalculationInteractionState();
        var folderState = CreateFolderState(calc);
        var templateRepository = new FakeTemplateRepository();
        var calcService = CreateGridCalculationService(folderState, interactionState, templateRepository);

        RegisterGridComponentServices(
            calcService,
            interactionState,
            folderState,
            templateRepository,
            CreateTaskService(),
            CreateResourceService());

        var cut = Render<CalcDataGrid>();

        cut.WaitForAssertion(() =>
            Assert.Contains("width:120px", cut.Find("#h2").GetAttribute("style")));

        await cut.InvokeAsync(() => cut.Instance.SaveTemplateBlazor("2||145"));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(145, calc.Template.NetCalc.Columns[0].Width);
            Assert.Contains("width:145px", cut.Find("#h2").GetAttribute("style"));
        });

        Assert.Equal(1, templateRepository.UpdateCalls);
    }

    [Fact]
    public void CalcDataGrid_TaskCollapse_HidesAndRestoresDescendants()
    {
        JSInterop.SetupVoid("initializeResizableColumns", _ => true);

        var calc = CreateGridCalculation();
        var interactionState = new CalculationInteractionState();
        var folderState = CreateFolderState(calc);
        var templateRepository = new FakeTemplateRepository();
        var calcService = CreateGridCalculationService(folderState, interactionState, templateRepository);

        RegisterGridComponentServices(
            calcService,
            interactionState,
            folderState,
            templateRepository,
            CreateTaskService(),
            CreateResourceService());

        var cut = Render<CalcDataGrid>();

        cut.WaitForAssertion(() =>
        {
            Assert.True(calc.Tasks[0].Ui.CollSpan);
            Assert.Single(cut.FindComponents<TaskRowComponent>());
            Assert.Single(cut.FindComponents<ResourceRowComponent>());
        });

        var taskComponent = cut.FindComponents<TaskRowComponent>().Single();
        taskComponent.Find("span[role='button']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.False(calc.Tasks[0].Ui.CollSpan);
            Assert.Single(cut.FindComponents<TaskRowComponent>());
            Assert.Empty(cut.FindComponents<ResourceRowComponent>());
        });

        taskComponent = cut.FindComponents<TaskRowComponent>().Single();
        taskComponent.Find("span[role='button']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(calc.Tasks[0].Ui.CollSpan);
            Assert.Single(cut.FindComponents<ResourceRowComponent>());
        });
    }

    [Fact]
    public void CalcDataGrid_GetFilter_ReducesVisibleRows()
    {
        JSInterop.SetupVoid("initializeResizableColumns", _ => true);

        var calc = CreateGridCalculation(includeSecondTask: true);
        var interactionState = new CalculationInteractionState();
        var folderState = CreateFolderState(calc);
        var templateRepository = new FakeTemplateRepository();
        var calcService = CreateGridCalculationService(folderState, interactionState, templateRepository);

        RegisterGridComponentServices(
            calcService,
            interactionState,
            folderState,
            templateRepository,
            CreateTaskService(),
            CreateResourceService());

        var cut = Render<CalcDataGrid>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindComponents<TaskRowComponent>().Count);
            Assert.Single(cut.FindComponents<ResourceRowComponent>());
        });

        calcService.GetFilter(new ProjectManagement.Client.Shared.ViewModel.FilterVM
        {
            Name = ["Task 2"],
            NameFilterType = ProjectManagement.Client.Shared.ViewModel.FilterType.equals
        });

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("#headerFilter"));
            Assert.Single(cut.FindComponents<TaskRowComponent>());
            Assert.Empty(cut.FindComponents<ResourceRowComponent>());
            Assert.Contains("Task 2", cut.Markup);
            Assert.DoesNotContain("Task 1", cut.Markup);
        });
    }

    [Fact]
    public void NelCalculationPage_ShowsStatusAndEmptyState_WhenCalculationHasNoTasks()
    {
        ComponentFactories.AddStub<CalculationToolbar>();
        ComponentFactories.AddStub<CalcDataGrid>();
        ComponentFactories.AddStub<GetFromStorage>();
        ComponentFactories.AddStub<VerticalSplitter>();

        var calc = CreateEmptyCalculation();
        var interactionState = new CalculationInteractionState();
        var folderState = CreateFolderState(calc);
        var calcService = CreateGridCalculationService(folderState, interactionState, new FakeTemplateRepository());

        RegisterPageComponentServices(calcService, folderState, interactionState);

        var cut = Render<NelCalculationPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='calculation-page-status']"));
            Assert.Single(cut.FindAll("[data-testid='calculation-page-empty']"));
            Assert.Contains("visibleItems", cut.Markup);
            Assert.Contains("calculationTableEmptyState", cut.Markup);
        });
    }

    [Fact]
    public void NelCalculationPage_ShowsNoVisibleRows_AndClearsFilters()
    {
        ComponentFactories.AddStub<CalculationToolbar>();
        ComponentFactories.AddStub<CalcDataGrid>();
        ComponentFactories.AddStub<GetFromStorage>();
        ComponentFactories.AddStub<VerticalSplitter>();

        var calc = CreateGridCalculation(includeSecondTask: true);
        calc.AllFlatItems = [];
        calc.FlatListDirty = false;
        calc.FilterVM = new FilterVM
        {
            Name = ["Task 99"],
            NameFilterType = FilterType.equals
        };

        var interactionState = new CalculationInteractionState();
        interactionState.SetModifierKey("Control");
        interactionState.HandleItemSelected(calc.Tasks[0].Id, calc.Tasks[0].Quantity, CalculationItemType.task);

        var folderState = CreateFolderState(calc);
        var calcService = CreateGridCalculationService(folderState, interactionState, new FakeTemplateRepository());

        RegisterPageComponentServices(calcService, folderState, interactionState);

        var cut = Render<NelCalculationPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("[data-testid='calculation-page-no-visible-rows']"));
            Assert.Contains("selectedItems", cut.Markup);
            Assert.Contains("clearFilters", cut.Markup);
        });

        cut.Find(".calc-state-action").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Null(calc.FilterVM);
            Assert.DoesNotContain("clearFilters", cut.Markup);
        });
    }

    [Fact]
    public void NelCalculationPage_StatusTogglesUpdateCalculationState()
    {
        ComponentFactories.AddStub<CalculationToolbar>();
        ComponentFactories.AddStub<CalcDataGrid>();
        ComponentFactories.AddStub<GetFromStorage>();
        ComponentFactories.AddStub<VerticalSplitter>();

        var calc = CreateGridCalculation(includeSecondTask: true);
        var interactionState = new CalculationInteractionState();
        interactionState.SetModifierKey("Control");
        interactionState.HandleItemSelected(calc.Tasks[0].Id, calc.Tasks[0].Quantity, CalculationItemType.task);

        var folderState = CreateFolderState(calc);
        var calcService = CreateGridCalculationService(folderState, interactionState, new FakeTemplateRepository());
        calcService.ShowComments = true;

        RegisterPageComponentServices(calcService, folderState, interactionState);

        var cut = Render<NelCalculationPage>();

        cut.Find("[data-testid='status-toggle-tasks']").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.False(calc.ShowTasks);
            Assert.Equal("false", cut.Find("[data-testid='status-toggle-tasks']").GetAttribute("aria-pressed")?.ToLowerInvariant());
        });

        cut.Find("[data-testid='status-toggle-resources']").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.False(calc.ShowResources);
            Assert.Equal("false", cut.Find("[data-testid='status-toggle-resources']").GetAttribute("aria-pressed")?.ToLowerInvariant());
        });

        cut.Find("[data-testid='status-toggle-comments']").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.False(calcService.ShowComments);
            Assert.Equal("false", cut.Find("[data-testid='status-toggle-comments']").GetAttribute("aria-pressed")?.ToLowerInvariant());
        });

        cut.Find("[data-testid='status-toggle-only-active']").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.True(calc.OnlyActive);
            Assert.Equal("true", cut.Find("[data-testid='status-toggle-only-active']").GetAttribute("aria-pressed")?.ToLowerInvariant());
        });

        cut.Find("[data-testid='status-clear-selection']").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Empty(interactionState.SelectedItems);
            Assert.Empty(cut.FindAll("[data-testid='status-clear-selection']"));
        });
    }

    private void RegisterCalculationComponentServices(
        FakeCalculationTableCoordinator coordinator,
        CalculationService calcService,
        FolderState folderState,
        CalculationInteractionState interactionState)
    {
        Services.AddSingleton<ICalculationTableCoordinator>(coordinator);
        Services.AddSingleton<IStringLocalizer<ResourceApp>>(new FakeStringLocalizer<ResourceApp>());
        Services.AddSingleton(calcService);
        Services.AddSingleton(folderState);
        Services.AddSingleton(interactionState);
    }

    private void RegisterPageComponentServices(
        CalculationService calcService,
        FolderState folderState,
        CalculationInteractionState interactionState)
    {
        Services.AddSingleton<IStringLocalizer<ResourceApp>>(new FakeStringLocalizer<ResourceApp>());
        Services.AddSingleton(calcService);
        Services.AddSingleton(folderState);
        Services.AddSingleton(interactionState);
    }

    private void RegisterRowComponentServices(
        CalculationInteractionState interactionState,
        CalculationService calcService,
        ClientResourceService? resourceService = null)
    {
        Services.AddSingleton(interactionState);
        Services.AddSingleton(calcService);
        Services.AddSingleton(resourceService ?? CreateResourceService());
    }

    private void RegisterGridComponentServices(
        CalculationService calcService,
        CalculationInteractionState interactionState,
        FolderState folderState,
        ITemplateRepository templateRepository,
        TaskService taskService,
        ClientResourceService resourceService)
    {
        Services.AddSingleton(calcService);
        Services.AddSingleton(interactionState);
        Services.AddSingleton(folderState);
        Services.AddSingleton(templateRepository);
        Services.AddSingleton(taskService);
        Services.AddSingleton(resourceService);
        Services.AddSingleton<IStringLocalizer<CalcResource>>(new FakeStringLocalizer<CalcResource>());
        Services.AddSingleton<ITaskRepository>(new FakeTaskRepository());
        Services.AddSingleton<IResourceTypeRepository>(new FakeResourceTypeRepository());
    }

    private static FolderState CreateFolderState(CalculationMVVM calculation)
    {
        var folderState = new FolderState();
        folderState.SetCalculation(calculation);
        return folderState;
    }

    private static CalculationService CreateCalculationService()
    {
        var calcService = (CalculationService)RuntimeHelpers.GetUninitializedObject(typeof(CalculationService));
        calcService.ShowComments = false;
        return calcService;
    }

    private static ClientResourceService CreateResourceService() =>
        (ClientResourceService)RuntimeHelpers.GetUninitializedObject(typeof(ClientResourceService));

    private static TaskService CreateTaskService() =>
        (TaskService)RuntimeHelpers.GetUninitializedObject(typeof(TaskService));

    private static CalculationService CreateGridCalculationService(
        FolderState folderState,
        CalculationInteractionState interactionState,
        ITemplateRepository templateRepository) =>
        new(
            templateRepository,
            new FakeCalculationRepository(),
            folderState,
            (MhdServices)RuntimeHelpers.GetUninitializedObject(typeof(MhdServices)),
            interactionState);

    private static CalculationMVVM CreateGridCalculation(bool includeSecondTask = false)
    {
        var task = new TaskListMVVM
        {
            Id = 1,
            Name = "Task 1",
            Resources = [],
            Tasks = []
        };
        task.Metadata = new TaskMetadata
        {
            IsActive = true,
            Code = "T-1",
            Quantity = 3
        };

        var resource = new ResourceListMVVM
        {
            Id = 11,
            TaskId = 1,
            Name = "Resource 1",
            Active = true,
            Status = "Has Offer",
            StatusColor = "#00aa00"
        };
        resource.Data = new ResourceMetadata
        {
            Quantity = 2,
            Cost = 10
        };

        task.Resources.Add(resource);

        List<TaskListMVVM> tasks = [task];

        if (includeSecondTask)
        {
            var task2 = new TaskListMVVM
            {
                Id = 2,
                Name = "Task 2",
                Resources = [],
                Tasks = []
            };
            task2.Metadata = new TaskMetadata
            {
                IsActive = true,
                Code = "T-2",
                Quantity = 4
            };

            tasks.Add(task2);
        }

        var calc = new CalculationMVVM
        {
            Id = 10,
            Tax = 25,
            Tasks = tasks,
            Template = new TemplateMVVM
            {
                StartCol1 = 40,
                NetCalc = new NetCalc
                {
                    Columns =
                    [
                        new NetColumnState { Id = NetColumnId.Name, Width = 120, Frozen = true },
                        new NetColumnState { Id = NetColumnId.Status, Width = 90, Frozen = false }
                    ]
                }
            }
        };

        calc.RebuildHierarchyAndIndexes();
        calc.AllFlatItems = calc.BuildFlatList();
        calc.FlatListDirty = false;
        return calc;
    }

    private static CalculationMVVM CreateGridCalculationWithAccountAndNameColumns()
    {
        var calc = CreateGridCalculation();
        calc.Template = new TemplateMVVM
        {
            StartCol1 = 40,
            NetCalc = new NetCalc
            {
                Columns =
                [
                    new NetColumnState { Id = NetColumnId.Account, Width = 70, Frozen = false },
                    new NetColumnState { Id = NetColumnId.Name, Width = 120, Frozen = false },
                    new NetColumnState { Id = NetColumnId.Status, Width = 90, Frozen = false }
                ]
            }
        };

        calc.RebuildHierarchyAndIndexes();
        calc.AllFlatItems = calc.BuildFlatList();
        calc.FlatListDirty = false;
        return calc;
    }

    private static CalculationMVVM CreateEmptyCalculation()
    {
        var calc = new CalculationMVVM
        {
            Id = 99,
            Tax = 25,
            Tasks = [],
            Template = new TemplateMVVM
            {
                StartCol1 = 40,
                NetCalc = new NetCalc
                {
                    Columns =
                    [
                        new NetColumnState { Id = NetColumnId.Name, Width = 120, Frozen = true }
                    ]
                }
            }
        };

        calc.RebuildHierarchyAndIndexes();
        calc.AllFlatItems = [];
        calc.FlatListDirty = false;
        return calc;
    }

    private static IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> CreateTestColumns() =>
        [
            new CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>
            {
                Id = NetColumnId.Name,
                TaskRender = static (builder, task) => RenderCell(builder, task.Name),
                ResRender = static (builder, resource) => RenderCell(builder, resource.Name)
            }
        ];

    private static void RenderCell(RenderTreeBuilder builder, string? value)
    {
        builder.OpenElement(0, "td");
        builder.AddContent(1, value ?? string.Empty);
        builder.CloseElement();
    }

    private static List<IElement> FindSpansByTitle(IRenderedComponent<CalculationToolbar> cut, string title) =>
        cut.FindAll("span")
            .Where(x => string.Equals(x.GetAttribute("title"), title, StringComparison.Ordinal))
            .ToList();

    private sealed class FakeCalculationTableCoordinator : ICalculationTableCoordinator
    {
        public bool CanPasteResult { get; set; }
        public int ClearOfferSelectionCalls { get; private set; }
        public int NotifyStructureRefreshCalls { get; private set; }
        public int UnselectAllCalls { get; private set; }
        public int PasteCalls { get; private set; }
        public int ShowTaskFormCalls { get; private set; }
        public TaskListMVVM? LastTaskFormModel { get; private set; }

        public bool CanPaste(CalculationItemType targetType) => CanPasteResult;
        public bool CanPasteIntoTask(TaskListMVVM task) => false;
        public void NotifyStructureRefresh() => NotifyStructureRefreshCalls++;
        public void ToggleOnlyActive() { }
        public void ToggleOH() { }
        public void UnselectAll() => UnselectAllCalls++;
        public void ClearOfferSelection() => ClearOfferSelectionCalls++;

        public Task PasteAsync(int taskId)
        {
            PasteCalls++;
            return Task.CompletedTask;
        }

        public void ShowTaskForm(TaskListMVVM model)
        {
            ShowTaskFormCalls++;
            LastTaskFormModel = model;
        }

        public void ShowResourceForm(ResourceListMVVM model) { }
        public void ShowImportDialog() { }
        public void ShowTemplateDialog() { }
        public void ShowTaskReorderDialog(TaskListMVVM? task = null) { }
        public void ShowQuantityDialog() { }
        public void ShowSaveToStorage(object item) { }
        public void ShowGetFromStorage(int parentId, CalculationItemType type) { }
    }

    private sealed class FakeStringLocalizer<T> : IStringLocalizer<T>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, $"{name} {string.Join(' ', arguments.Select(x => x?.ToString()))}".Trim(), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Enumerable.Empty<LocalizedString>();
    }

    private sealed class FakeTemplateRepository : ITemplateRepository
    {
        public int UpdateCalls { get; private set; }

        public Task<List<TemplateMVVM>> GetAsync(int? department = null) => Task.FromResult(new List<TemplateMVVM>());
        public Task<TemplateMVVM> GetByIdAsync(int id) => Task.FromResult(new TemplateMVVM());
        public Task<TemplateMVVM> SetDefaultAsync(int calcID, int? newTmplateId) => Task.FromResult(new TemplateMVVM());
        public Task<TemplateMVVM> CreateAsync(TemplateListPostDTO template) => Task.FromResult(new TemplateMVVM());
        public Task<TemplateMVVM> CreateAsync(int? departmentId, TemplateListPostDTO template) => Task.FromResult(new TemplateMVVM());
        public Task<bool> UpdateAsync(TemplateListPostDTO temp, int id)
        {
            UpdateCalls++;
            return Task.FromResult(true);
        }
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        public Task<List<ListOrderDTO>> GetAsync(int? id = null) => Task.FromResult(new List<ListOrderDTO>());
        public Task<bool> ReOrderAsync(int Id, int newOrder) => Task.FromResult(true);
        public Task<bool> CreateAsync(List<TaskPostDTO> model, int NetCalc) => Task.FromResult(true);
        public Task<List<TaskListMVVM>> GetByFilterAsync(FilterCalculationItemsDto offer) => Task.FromResult(new List<TaskListMVVM>());
        public Task<bool> UpdateAsync(TaskPostDTO model, int id) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int calcID, IEnumerable<int> items) => Task.FromResult(true);
    }

    private sealed class FakeResourceTypeRepository : IResourceTypeRepository
    {
        public Task<List<ListResourceTypeDTO>> GetLocalAsync() => Task.FromResult(new List<ListResourceTypeDTO>());
        public Task<List<ListResourceTypeDTO>> GetVisualResourcesAsync() => Task.FromResult(new List<ListResourceTypeDTO>());
        public Task<List<ResourceSortModel>> GetResourceSortAsync(int resourceId) => Task.FromResult(new List<ResourceSortModel>());
    }

    private sealed class FakeCalculationRepository : ICalculationRepository
    {
        public Task<bool> ReOrderAsync(int Id, int newOrder) => Task.FromResult(true);
        public Task<int> CopyAsync(Guid ProjectId, int calcId) => Task.FromResult(0);
        public Task<GetProjectCalcConfigDTO> GetConfig(int? m, int? con, int? com, int? t, int? st) => Task.FromResult(new GetProjectCalcConfigDTO());
        public Task<List<ListCalculationMVVM>> GetAsync(Guid projectId, bool isVisible = true) => Task.FromResult(new List<ListCalculationMVVM>());
        public Task<List<HourlyPriceListGroupDTO>> GetHourlyPriceListAsync(int calcid) => Task.FromResult(new List<HourlyPriceListGroupDTO>());
        public Task<List<ListCalculationMVVM>> GetShareCalculationsAsync(Guid projectId) => Task.FromResult(new List<ListCalculationMVVM>());
        public Task<CalculationDetailsDTO> DetailsAsync(int id) => Task.FromResult(new CalculationDetailsDTO());
        public Task<CalculationMVVM> GetPageAsync(int id, bool otherdepartment) => Task.FromResult(new CalculationMVVM());
        public Task<CalculationPostDTO> GetPostAsync(int id) => Task.FromResult(new CalculationPostDTO());
        public Task<int> CreateAsync(Guid ProjectId, CalculationPostDTO model) => Task.FromResult(0);
        public Task<bool> UpdateAsync(int calculationId, List<HourlyPriceListGroupDTO> hourlyPriceList) => Task.FromResult(true);
        public Task<bool> UpdateAsync(CalculationPostDTO model, int id) => Task.FromResult(true);
        public Task<bool> UpdateAsync(List<OHFactors> model, int id) => Task.FromResult(true);
        public Task<bool> UpdateAsync(List<QuanityListDTO> model, int id) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
    }
}

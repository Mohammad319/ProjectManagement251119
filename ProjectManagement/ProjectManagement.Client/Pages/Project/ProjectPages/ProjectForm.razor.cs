using BlazorMHD.UI.Core.Navigation;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles.Identity;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Pages.Project.ProjectPages
{
    public partial class ProjectForm : AppComponentBase
    {
        public const string DialogFormId = "projectForm";
        int Part = 1;

        [Parameter] public EventCallback<Tuple<bool, ListProjectMVVM>> Callback { get; set; }
        [Parameter] public required ListProjectMVVM Project { get; set; }
        [Parameter] public Guid FolderId { get; set; }

        bool IsLoading = true;

        PostProjectDTO ProjectUpdate = new();
        GetProjectCalcConfigDTO? Config;

        private record ProjectTab(int Id, string Label);

        private List<ProjectTab> Tabs => new()
        {
            new(1, CalcResource.project),
            new(2, CalcResource.procurement),
            new(3, ResourceApp.organisation),
            new(4, ResourceIdentity.customerGroup),
            new(5, ResourceApp.date),
            new(6, CalcResource.note),
            new(7, ResourceIdentity.contact)
        };

        // الربط مع MhdTabs
        string ActiveKey
        {
            get => Part.ToString();
            set
            {
                if (int.TryParse(value, out var p))
                    Part = p;
            }
        }
        private List<TabItem> TabItems =>
            Tabs.Select(t => new TabItem
            {
                Key = t.Id.ToString(),
                Text = t.Label,
                Icon = GetTabIcon(t.Id)
            }).ToList();



        private bool PartCompleted(int id)
        {
            if (ProjectUpdate == null) return false;

            return id switch
            {
                1 => !string.IsNullOrWhiteSpace(ProjectUpdate.Code)
                     && !string.IsNullOrWhiteSpace(ProjectUpdate.Name)
                     && ProjectUpdate.TypeId > 0,

                2 => ProjectUpdate.ContractId > 0
                     && ProjectUpdate.CompensationId > 0
                     && ProjectUpdate.ProcurementMethodsId > 0,

                3 => ProjectUpdate.Responsibles?.Any(r => !string.IsNullOrWhiteSpace(r)) == true,

                4 => ProjectUpdate.OrganisationId > 0
                     || !string.IsNullOrWhiteSpace(ProjectUpdate.Developer),

                5 => ProjectUpdate.StartDate != default
                     && ProjectUpdate.EndDate != default,

                6 => ProjectUpdate.Notes?.Any(n => !string.IsNullOrWhiteSpace(n)) == true,

                7 => ProjectUpdate.Contacts?.Any() == true,

                _ => false
            };
        }

        protected override async Task OnInitializedAsync()
        {
            ProjectUpdate.FolderId = FolderId;

            if (Project.Id != Guid.Empty)
            {
                ProjectUpdate = await Repo.Project.GetToPostAsync(Project.Id)?? new PostProjectDTO();
            }

            ProjectUpdate.Notes ??= [];
            ProjectUpdate.Responsibles ??= [];
            ProjectUpdate.Contacts ??= [];

            Config = await
                Repo.Project.GetConfig(
                    ProjectUpdate.ProcurementMethodsId,
                    ProjectUpdate.ContractId,
                    ProjectUpdate.CompensationId,
                    ProjectUpdate.TypeId);

            IsLoading = false;
        }

        private async Task HandleSubmitAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;

            PostProjectDTO entity = new();
            PropertyCopier.CopyPropertiesTo(ProjectUpdate, entity);

            var resultInfo = Tuple.Create(ProjectUpdate.IsVisible, new ListProjectMVVM());
            PropertyCopier.CopyPropertiesTo(ProjectUpdate, resultInfo.Item2);

            bool result;

            if (Project.Id != Guid.Empty)
                result = await
                    Repo.Project.UpdateAsync(Project.Id, entity);
            else
            {
                resultInfo.Item2.Id = await Repo.Project.CreateAsync(entity);

                result = resultInfo.Item2.Id != Guid.Empty;
            }

            if (result)
                await Callback.InvokeAsync(resultInfo);

            MHD.Notifications(Project.Id != Guid.Empty ? ToastType.Update : ToastType.Add, result);

            IsLoading = false;
        }
        private string GetTabIcon(int id)
        {
            return id switch
            {
                1 => "🧾",   // المعلومات الأساسية
                2 => "📑",   // العقد والتعويض
                3 => "👤",   // المسؤوليات
                4 => "🏢",   // المنظمة والجهات
                5 => "📅",   // التواريخ
                6 => "📝",   // الملاحظات
                7 => "📞",   // الأشخاص – Contacts
                _ => ""
            };
        }

    }
}

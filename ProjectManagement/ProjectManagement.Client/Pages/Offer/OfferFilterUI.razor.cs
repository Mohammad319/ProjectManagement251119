using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.MVVM.Offer;
using ProjectManagement.Client.Shared.Repositories;
using ProjectManagement.Shared.DTO.Offer;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.DTO.ResourceType;

namespace ProjectManagement.Client.Pages.Offer
{
    public partial class OfferFilterUI
    {
        [Parameter] public CalculationItemType ItemType { get; set; }
        [Parameter] public EventCallback Callback { get; set; }
        public PostStorygeDTO Post;
        private OfferFilterDTO Filter = new();
        private List<ResourceSortModel> ResourcesSort { get; set; }
        List<ListResourceTypeDTO> ResourceTypes { get; set; }
        List<ListOrderDTO> Statues;
        List<ListOrderDTO> TGSStatuses;
        List<FolderMVVM> FoldersList = [];
        ListResourceTypeDTO ResTypeSelected;
        List<ListProjectMVVM> Projects;
        List<ListCalculationMVVM> Calcs;
        List<ListDTO> Organisations;
        [Inject] HTTPRepository _httpRepository { get; set; }
        private void OnInputOrgChanged(ChangeEventArgs e)
        {
            var selectedName = e.Value?.ToString();
            var selectedOrg = Organisations.FirstOrDefault(o => o.Name == selectedName);
            if (selectedOrg != null)
            {
                Filter.OrganisationId = selectedOrg.Id;
            }
            else
            {
                Filter.OrganisationId = null; // أو 0 مثلاً
            }
        }
        public List<ListOfferCalcInfoMVVM> Offers { get; set; } = [];

        public async Task SetNewTypeAsync(CalculationItemType nt)
        {
            Offers = null;
            Post.Type = nt;
            Post.Items = [];
            await NewIni();
            StateHasChanged();
        }

        public async Task<bool> SaveAsync(int ID)
        {
            Post.ParentID = ID;
            if (Post.Items.Count == 0) return false;
            return await Repo.Storage.CreateItem(Post);
        }

        public async Task<int> GetOffersAsync()
        {
            Offers = null;
            Offers = await Repo.Offer.GetByFilterAsync(Filter);

            StateHasChanged();
            return 0;
        }
        async Task ChangeResType(int? id)
        {
            if (!id.HasValue)
                return;

            ResTypeSelected = ResourceTypes.FirstOrDefault(x => x.Id == id);
            ResourcesSort = null;
            ResourcesSort = await Repo.ResType.GetResourceSortAsync(id.Value);
        }

        void CalcChanged(int? id)
        {
            Filter.CalculationID = id;
        }
        async Task ProjectChanged(Guid? id)
        {
            Filter.ProjectID = id;
            Filter.CalculationID = 0;
            if (id.HasValue)
            {
                Calcs = await Repo.Calculation.GetAsync(id.Value);
            }
        }
        async Task FolderChanged(Guid? id)
        {
            Filter.FolderID = id;
            Filter.ProjectID = null;
            Filter.CalculationID = 0;

            Projects = null;
            if (id.HasValue)
            {
                Projects = await Repo.Project.GetByFolderIdAsync(id.Value);
                Projects ??= await Repo.Project.GetOtherDepartmentAsync(id.Value);
            }
        }

        async Task NewIni()
        {
            if (Post.Type == CalculationItemType.task)
            {
                ResourceTypes = await Repo.ResType.GetLocalAsync();
                Statues = [.. (await _httpRepository.GetAsync<List<ListOrderDTO>>(PMAPIConst.ResourceStatus + $"?id={null}"))]; ;
            }
            else
            {
                TGSStatuses = [.. await Repo.Task.GetAsync()];
            }
        }
        protected async override Task OnInitializedAsync()
        {
            Post = new()
            {
                copyType = CopyType.Copy,
                NewCalcID = Calc.Id,
                IsOH = Calc.OHFactors,
                WithCildren = true,
                Type = ItemType,
                Items = [],

            };
            FoldersList = await Repo.Folder.GetByVisible(true);
            Organisations = await Repo.Org.GetVisibleOrIdAsync();
        }
    }
}

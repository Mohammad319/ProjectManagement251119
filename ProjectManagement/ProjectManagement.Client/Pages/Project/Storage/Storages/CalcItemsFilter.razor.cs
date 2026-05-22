using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.DTO.ResourceType;
using ProjectManagement.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Pages.Project.Storage.Storages
{
    public partial class CalcItemsFilter
    {
        [Parameter] public CalculationItemType ItemType { get; set; }
        [Parameter] public EventCallback Callback { get; set; }
        [Parameter] public int ParentID { get; set; }
        [Parameter] public int MaxFractionDigits { get; set; } = NumericFormatHelper.DefaultMaxFractionDigits;

        object? List;
        FilterCalculationItemsDto Filter = new() { Page = -1 };
        List<AccountModel> Accounts { get; set; } = [];
        List<ResourceSortModel> ResourcesSort { get; set; } = [];
        List<ListOrderDTO> TGSStatuses = [];
        List<FolderMVVM> FoldersList = [];
        ListResourceTypeDTO? ResTypeSelected;
        AccountGroupsListDto AccountGroupSelected = new();
        List<ListProjectMVVM> Projects = [];
        List<ListCalculationMVVM> Calcs = [];
        public PostStorygeDTO Post = new();
        public async Task SetNewTypeAsync(CalculationItemType nt)
        {
            List = null;
            Post.Type = nt;
            Post.Items = [];
            await NewIni();
            StateHasChanged();
        }

        public async Task<bool> SaveAsync(int ID)
        {
            Post.ParentID = ID;

            Post.NewCalcID = Calc.Id;

            if (Post.Items.Count == 0) return false;
            return await Repo.Storage.CreateItem(Post);
        }
        async Task AddAsync(int id, decimal? q)
        {
            if (Post.Items.Any(x => x.Id == id))
            {
                var qr = Post.Items.FirstOrDefault(x => x.Id == id);
                if (qr is not null)
                {
                    Post.Items.Remove(qr);
                }
            }
            else Post.Items.Add(new ResourceTaskItemDTO(id, q));
            await Callback.InvokeAsync();
        }
        public async Task<int> GetObjectTypeAsync(int n)
        {
            List = null;
            //int num;
            Filter.Page = Filter.Page + n;
            if (Post.Type == CalculationItemType.task)
                List = await Repo.Task.GetByFilterAsync(Filter);
            else
                List = await Repo.Resource.GetByFilterAsync(Filter);
            StateHasChanged();
            return 0;
        }
        async Task ChangeResType(int? id)
        {
            if (!id.HasValue)
                return;

            ResTypeSelected = Config.ResourceTypes?.FirstOrDefault(x => x.Id == id);
            ResourcesSort = [];
            ResourcesSort = await Repo.ResType.GetResourceSortAsync(id.Value);
        }
        async Task ChangeGroupAccount(int id)
        {
            AccountGroupSelected = new();
            Filter.AccountGroup = id;
            await Task.Delay(1);
            AccountGroupSelected = Config.AccountGroups?.FirstOrDefault(x => x.Id == id) ?? new();
        }
        void CalcChanged(int id)
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

            Projects = [];
            if (id.HasValue)
            {
                Projects = Folder.State.OtherDepartment
                    ? await Repo.Project.GetOtherDepartmentAsync(id.Value)
                    : await Repo.Project.GetByFolderIdAsync(id.Value);
            }
        }
        ResourceFormDTO Config = new();

        async Task NewIni()
        {
            if (Post.Type == CalculationItemType.task)
            {
                Config = await Repo.Resource.GetConfigForm();
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
            FoldersList = await Repo.Folder.GetAllVisibleAsync();
        }

        private string FormatNumber(decimal value)
            => NumericFormatHelper.Format(value, MaxFractionDigits);

        private string FormatNumber(decimal? value)
            => NumericFormatHelper.Format(value, MaxFractionDigits);

        private string FormatNumber(double? value)
            => NumericFormatHelper.Format(value, MaxFractionDigits);
    }
}

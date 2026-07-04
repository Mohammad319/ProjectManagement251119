using Application.Feature.Organisation.Organisation.Commands;
using Application.Feature.Organisation.Organisation.Queries;
using Application.Feature.Organisation.OrganisationCategory.Queries;
using Domain.DTO.Category;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ProjectManagement.Client.Constant;
using ProjectManagement.Client.Helper;
using ProjectManagement.Components.ControlComponents.Organisation.Organisation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Organisation;

namespace ProjectManagement.Components.ControlComponents.Organisation.OrganisationCategory
{
    // "Kunder & leverantörer": flat, sortable/filterable table of companies/persons across all
    // huvudgrupper (top-level categories) and underkategorier (child categories). Replaces the old
    // left-tree + right-panel layout. Follows the same fixed-toolbar / scrolling-list pattern as the
    // Projekt/Kalkyl admin lists (pm-admin-panel), with a sticky table header and pagination.
    public partial class CategoriesUI
    {
        [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;

        private const string ColumnStorageKey = "kl-org-columns";

        private bool _loading = true;
        private bool _canManage;

        private List<OrganisationRowDTO> _all = [];
        private List<ListOrganisationCategoryDTO> _categories = [];

        // Filters
        private string _search = string.Empty;
        private int? _mainGroupFilter;
        private int? _subCategoryFilter;
        private string? _statusFilter;
        private ActiveFilter _activeFilter = ActiveFilter.Active;

        // Sorting
        private OrgSortColumn _sort = OrgSortColumn.Name;
        private bool _sortDesc;

        // Paging
        private int _page = 1;
        private int _pageSize = 25;
        private static readonly int[] PageSizeOptions = [10, 25, 50, 100];

        // Column show/hide (only the optional columns can be toggled)
        private HashSet<OrgColumn> _visibleColumns = DefaultColumns();
        private bool _columnMenuOpen;

        private static readonly (OrgColumn Col, string Label)[] HideableColumns =
        [
            (OrgColumn.OrgNr, "Organisationsnummer"),
            (OrgColumn.Email, "E-post"),
            (OrgColumn.Phone, "Telefon"),
            (OrgColumn.City, "Stad"),
            (OrgColumn.Country, "Land"),
            (OrgColumn.Status, "Status"),
            (OrgColumn.UpdatedAt, "Senast ändrad"),
        ];

        private static HashSet<OrgColumn> DefaultColumns() =>
            [OrgColumn.Email, OrgColumn.Phone, OrgColumn.Status];

        protected override async Task OnInitializedAsync()
        {
            _canManage = await CanManageAsync();
            await LoadAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
                return;

            try
            {
                var stored = await JS.InvokeAsync<string?>("localStorage.getItem", ColumnStorageKey);
                if (!string.IsNullOrWhiteSpace(stored))
                {
                    var set = new HashSet<OrgColumn>();
                    foreach (var part in stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        if (Enum.TryParse<OrgColumn>(part, out var col))
                            set.Add(col);

                    _visibleColumns = set;
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch
            {
                // localStorage unavailable (prerender/JS disconnected) — keep defaults.
            }
        }

        private async Task LoadAsync()
        {
            _loading = true;
            await InvokeAsync(StateHasChanged);

            _categories = await Dispatcher.Send(new GetOrganisationCategoryQuery()) ?? [];
            _all = await Dispatcher.Send(new GetOrganisationRowsQuery(true)) ?? [];

            _loading = false;
            await InvokeAsync(StateHasChanged);
        }

        private async Task<bool> CanManageAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            if (user.Identity?.IsAuthenticated != true)
                return false;

            return PMRolesConst.Tenant.AdminManger
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(user.IsInRole);
        }

        // ---------------- Derived collections ----------------

        private IEnumerable<ListOrganisationCategoryDTO> MainGroups
            => _categories.Where(x => x.ParentCategoryId is null).OrderBy(x => x.Name);

        private IEnumerable<ListOrganisationCategoryDTO> SubCategoriesForFilter
            => _mainGroupFilter is null
                ? _categories.Where(x => x.ParentCategoryId is not null).OrderBy(x => x.Name)
                : _categories.Where(x => x.ParentCategoryId == _mainGroupFilter).OrderBy(x => x.Name);

        private IEnumerable<string> StatusOptions
            => _all.Where(x => !string.IsNullOrWhiteSpace(x.Status))
                   .Select(x => x.Status.Trim())
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .OrderBy(x => x);

        private List<OrganisationRowDTO> Filtered()
        {
            IEnumerable<OrganisationRowDTO> q = _all;

            q = _activeFilter switch
            {
                ActiveFilter.Active => q.Where(x => x.IsVisible),
                ActiveFilter.Archived => q.Where(x => !x.IsVisible),
                _ => q
            };

            if (_mainGroupFilter is not null)
                q = q.Where(x => x.MainGroupId == _mainGroupFilter);

            if (_subCategoryFilter is not null)
                q = q.Where(x => x.SubCategoryId == _subCategoryFilter);

            if (!string.IsNullOrWhiteSpace(_statusFilter))
                q = q.Where(x => string.Equals(x.Status?.Trim(), _statusFilter, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(_search))
            {
                var s = _search.Trim();
                q = q.Where(x =>
                    Contains(x.Name, s) ||
                    Contains(x.OrganisationNumber, s) ||
                    Contains(x.Email, s) ||
                    Contains(x.Phone, s) ||
                    Contains(x.City, s));
            }

            q = _sort switch
            {
                OrgSortColumn.MainGroup => Order(q, x => x.MainGroup),
                OrgSortColumn.SubCategory => Order(q, x => x.SubCategory),
                OrgSortColumn.City => Order(q, x => x.City),
                OrgSortColumn.Status => Order(q, x => x.Status),
                OrgSortColumn.UpdatedAt => _sortDesc
                    ? q.OrderByDescending(x => x.UpdatedAt)
                    : q.OrderBy(x => x.UpdatedAt),
                _ => Order(q, x => x.Name)
            };

            return q.ToList();
        }

        private IEnumerable<OrganisationRowDTO> Order(IEnumerable<OrganisationRowDTO> q, Func<OrganisationRowDTO, string> key)
            => _sortDesc
                ? q.OrderByDescending(key, StringComparer.OrdinalIgnoreCase)
                : q.OrderBy(key, StringComparer.OrdinalIgnoreCase);

        private static bool Contains(string? value, string term)
            => !string.IsNullOrEmpty(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);

        // Paging helpers computed against the filtered set
        private List<OrganisationRowDTO> _pageCache = [];
        private int _totalRows;
        private int TotalPages => Math.Max(1, (int)Math.Ceiling(_totalRows / (double)_pageSize));

        private List<OrganisationRowDTO> PagedRows()
        {
            var filtered = Filtered();
            _totalRows = filtered.Count;

            if (_page > TotalPages)
                _page = TotalPages;
            if (_page < 1)
                _page = 1;

            _pageCache = filtered.Skip((_page - 1) * _pageSize).Take(_pageSize).ToList();
            return _pageCache;
        }

        // ---------------- Interaction ----------------

        private void SortBy(OrgSortColumn column)
        {
            if (_sort == column)
                _sortDesc = !_sortDesc;
            else
            {
                _sort = column;
                _sortDesc = false;
            }
            _page = 1;
        }

        private void OnMainGroupChanged(ChangeEventArgs e)
        {
            _mainGroupFilter = ParseNullableInt(e.Value?.ToString());
            _subCategoryFilter = null;
            _page = 1;
        }

        private void OnSubCategoryChanged(ChangeEventArgs e)
        {
            _subCategoryFilter = ParseNullableInt(e.Value?.ToString());
            _page = 1;
        }

        private void OnStatusChanged(ChangeEventArgs e)
        {
            var v = e.Value?.ToString();
            _statusFilter = string.IsNullOrWhiteSpace(v) ? null : v;
            _page = 1;
        }

        private void SetActiveFilter(ActiveFilter filter)
        {
            _activeFilter = filter;
            _page = 1;
        }

        private void OnSearchInput(ChangeEventArgs e)
        {
            _search = e.Value?.ToString() ?? string.Empty;
            _page = 1;
        }

        private void SetPageSize(int size)
        {
            _pageSize = size;
            _page = 1;
        }

        private void GoToPage(int page)
        {
            _page = Math.Clamp(page, 1, TotalPages);
        }

        private bool IsColumnVisible(OrgColumn col) => _visibleColumns.Contains(col);

        private async Task ToggleColumn(OrgColumn col)
        {
            if (!_visibleColumns.Remove(col))
                _visibleColumns.Add(col);

            try
            {
                await JS.InvokeVoidAsync("localStorage.setItem", ColumnStorageKey,
                    string.Join(',', _visibleColumns.Select(x => x.ToString())));
            }
            catch
            {
                // Non-fatal — column choice still applies for this session.
            }
        }

        // ---------------- Actions ----------------

        private void OpenDetails(OrganisationRowDTO row)
        {
            if (row.Id <= 0)
                return;

            MHD.Modal.ShowComponent<OrganisationDetailsUI>(
                row.Name,
                new Dictionary<string, object>
                {
                    [nameof(OrganisationDetailsUI.CompanyId)] = row.Id
                },
                BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge);
        }

        private void OpenCreate() => OpenForm(0, "Ny kund/leverantör");

        private void OpenEdit(OrganisationRowDTO row) => OpenForm(row.Id, AppLoc[LocalizerConst.Update, row.Name]);

        private void OpenForm(int id, string title)
        {
            MHD.Modal.ShowComponent<OrganisationFormUI>(
                title,
                new Dictionary<string, object>
                {
                    [nameof(OrganisationFormUI.ID)] = id,
                    [nameof(OrganisationFormUI.CategoryID)] = _subCategoryFilter ?? 0,
                    [nameof(OrganisationFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, OnFormClosed)
                },
                BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge,
                DialogButtonsHelper.CreateSaveCancelButtons(OrganisationFormUI.DialogFormId));
        }

        private async Task OnFormClosed(bool changed)
        {
            if (changed)
                await LoadAsync();

            await MHD.Modal.CloseAsync();
            await InvokeAsync(StateHasChanged);
        }

        private void OpenCategoryManager()
        {
            MHD.Modal.ShowComponent<CategoryManagerUI>(
                "Hantera kategorier",
                new Dictionary<string, object>
                {
                    [nameof(CategoryManagerUI.OnChanged)] = EventCallback.Factory.Create(this, LoadAsync)
                },
                BlazorMHD.UI.Core.Services.MhdDialogSize.Large);
        }

        private async Task ArchiveAsync(OrganisationRowDTO row, bool archive)
        {
            var result = await Dispatcher.Send(new ArchiveOrganisationCommand(row.Id, archive));
            if (result)
            {
                row.IsVisible = !archive;
                MHD.Notifications(ToastType.Update, true);
            }
            else
            {
                MHD.Notifications(ToastType.Update, false);
            }

            await InvokeAsync(StateHasChanged);
        }

        private void ConfirmDelete(OrganisationRowDTO row)
        {
            if (row.IsUsed)
                return;

            MHD.DeleteMessage(row.Name, EventCallback.Factory.Create(this, () => DeleteAsync(row)));
        }

        private async Task DeleteAsync(OrganisationRowDTO row)
        {
            var result = await Dispatcher.Send(new DeleteOrganisationCommand(row.Id));
            if (result)
                _all.RemoveAll(x => x.Id == row.Id);

            MHD.Notifications(ToastType.Delete, result);
            await InvokeAsync(StateHasChanged);
        }

        private static int? ParseNullableInt(string? value)
            => int.TryParse(value, out var parsed) && parsed > 0 ? parsed : null;

        public enum OrgSortColumn { Name, MainGroup, SubCategory, City, Status, UpdatedAt }
        public enum OrgColumn { OrgNr, Email, Phone, City, Country, Status, UpdatedAt }
        public enum ActiveFilter { Active, Archived, All }
    }
}

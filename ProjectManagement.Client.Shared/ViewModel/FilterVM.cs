using ProjectManagement.Shared.Enums;
using System.Collections.Generic;

namespace ProjectManagement.Client.Shared.ViewModel
{
    public enum FilterType
    {
        equals, noEquals, contains, startsWith, endsWith, doesNotContains
    }
    public class FilterVM
    {
        public bool FilterVisible { get; set; }
        public List<string> Code { get; set; } = [];
        public FilterType CodeFilterType { get; set; } = FilterType.equals;

        public List<string> Name { get; set; } = [];
        public FilterType NameFilterType { get; set; } = FilterType.equals;

        public List<string> Account { get; set; } = [];
        public FilterType AccountFilterType { get; set; } = FilterType.equals;

        public List<string> Status { get; set; } = [];
        public FilterType StatusFilterType { get; set; } = FilterType.equals;

        public int ResourceTypeId { get; set; }
        public List<string> Resource { get; set; } = [];//to remove
        public FilterType ResourceFilterType { get; set; } = FilterType.equals;
        public List<ResourceTypesEnum> ResourceTypeSystem { get; set; } = [];
        public FilterType ResourceTypeFilterTypeSystem { get; set; } = FilterType.equals;

        public int? ResourceSortId { get; set; }
        public List<string> ResourceSort { get; set; } = [];//to remove
        public FilterType ResourceSortFilterType { get; set; } = FilterType.equals;

        public List<string> Unit { get; set; } = [];
        public FilterType UnitFilterType { get; set; } = FilterType.equals;

    }
}

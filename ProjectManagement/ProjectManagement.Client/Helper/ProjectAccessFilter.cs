using System;
using System.Collections.Generic;
using System.Linq;
using ProjectManagement.Client.Shared.Model.Filter;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Helper;

namespace ProjectManagement.Client.Helper
{
    /// <summary>
    /// Pure logic for the project list's detailed "Åtkomst" filter. The Åtkomst COLUMN stays simple
    /// (Avdelningsåtkomst / Delat / Delvis delat), but the FILTER is faceted across four groups —
    /// Åtkomsttyp, Åtkomstnivå (Kan visa / Kan ändra), Personer and Avdelningar. Matching is
    /// OR within a group and AND across groups. Kept out of the .razor so it can be unit-tested.
    /// </summary>
    public static class ProjectAccessFilter
    {
        /// <summary>
        /// Builds the filter groups from the projects currently in the list. The Personer/Avdelningar
        /// groups list the distinct share recipients found across those projects; they are omitted when
        /// there are none. Headers use normal Swedish casing (from <see cref="AccessSummaryFormatter"/>).
        /// </summary>
        public static List<FilterOptionGroup> BuildGroups(IEnumerable<ListProjectMVVM> projects)
        {
            static FilterOption Opt(string s) => new() { Value = s, Label = s };

            var recipients = projects
                .Where(p => p.Access is not null)
                .SelectMany(p => p.Access!.Recipients)
                .ToList();

            List<string> Names(ProjectShareRecipientType type) => recipients
                .Where(r => r.Type == type)
                .Select(r => r.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            var persons = Names(ProjectShareRecipientType.User);
            var departments = Names(ProjectShareRecipientType.Department);

            var groups = new List<FilterOptionGroup>
            {
                new()
                {
                    Header = AccessSummaryFormatter.GroupAccessType,
                    Options =
                    [
                        Opt(AccessSummaryFormatter.ProjectTypeDepartmentOnly),
                        Opt(AccessSummaryFormatter.TypeShared),
                        Opt(AccessSummaryFormatter.ProjectTypeLimited),
                    ]
                },
                new()
                {
                    Header = AccessSummaryFormatter.GroupAccessLevel,
                    Options =
                    [
                        Opt(AccessSummaryFormatter.LevelView),
                        Opt(AccessSummaryFormatter.LevelEdit),
                    ]
                },
            };

            if (persons.Count > 0)
                groups.Add(new() { Header = AccessSummaryFormatter.GroupPersons, Options = persons.Select(Opt).ToList() });
            if (departments.Count > 0)
                groups.Add(new() { Header = AccessSummaryFormatter.GroupDepartments, Options = departments.Select(Opt).ToList() });

            return groups;
        }

        /// <summary>The values a project matches within a given filter group.</summary>
        public static IReadOnlyCollection<string> ValuesForGroup(ListProjectMVVM p, string header)
        {
            if (header == AccessSummaryFormatter.GroupAccessType)
                return AccessSummaryFormatter.ProjectTags(p.Access, p.IsShared).ToList();

            var recipients = p.Access?.Recipients;
            if (recipients is null || recipients.Count == 0)
                return Array.Empty<string>();

            if (header == AccessSummaryFormatter.GroupAccessLevel)
                return recipients.Select(r => AccessSummaryFormatter.AccessLevelLabel(r.Role)).Distinct().ToList();
            if (header == AccessSummaryFormatter.GroupPersons)
                return recipients.Where(r => r.Type == ProjectShareRecipientType.User).Select(r => r.Name).ToList();
            if (header == AccessSummaryFormatter.GroupDepartments)
                return recipients.Where(r => r.Type == ProjectShareRecipientType.Department).Select(r => r.Name).ToList();

            return Array.Empty<string>();
        }

        /// <summary>
        /// True when <paramref name="p"/> satisfies the selection: within every group that has at least
        /// one selected value the project must match one of them (OR within), and every such group must
        /// be satisfied (AND across). Groups with no selected value do not constrain the result.
        /// </summary>
        public static bool Matches(ListProjectMVVM p, IReadOnlyCollection<FilterOptionGroup> groups, ICollection<string> selected)
        {
            foreach (var group in groups)
            {
                var selectedInGroup = group.Options.Select(o => o.Value).Where(selected.Contains).ToList();
                if (selectedInGroup.Count == 0)
                    continue;

                var values = ValuesForGroup(p, group.Header);
                if (!selectedInGroup.Any(values.Contains))
                    return false;
            }

            return true;
        }

        /// <summary>Convenience: build the groups from <paramref name="projects"/> and filter that same set.</summary>
        public static IEnumerable<ListProjectMVVM> Apply(IEnumerable<ListProjectMVVM> projects, ICollection<string> selected)
        {
            var list = projects as IList<ListProjectMVVM> ?? projects.ToList();
            if (selected.Count == 0)
                return list;

            var groups = BuildGroups(list);
            return list.Where(p => Matches(p, groups, selected));
        }
    }
}

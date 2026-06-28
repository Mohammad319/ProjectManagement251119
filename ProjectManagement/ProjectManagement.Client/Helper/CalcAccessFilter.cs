using System;
using System.Collections.Generic;
using System.Linq;
using ProjectManagement.Client.Shared.Model.Filter;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Helper;

namespace ProjectManagement.Client.Helper
{
    /// <summary>
    /// Pure logic for the calculation list's detailed "Åtkomst" filter. The Åtkomst COLUMN stays simple
    /// (Privat / Via projekt / Avdelningsåtkomst – never "Ej via projekt"), but the FILTER is faceted
    /// across four groups — Åtkomsttyp, Åtkomstnivå (Kan visa / Kan ändra), Personer and Avdelningar.
    /// Matching is OR within a group and AND across groups. Mirrors <see cref="ProjectAccessFilter"/> so
    /// the two lists behave identically; kept out of the .razor so it can be unit-tested.
    /// </summary>
    public static class CalcAccessFilter
    {
        /// <summary>
        /// Builds the filter groups from the calculations currently in the list. The Personer/Avdelningar
        /// groups list the distinct share recipients found across those calculations (sharing happens at
        /// project level, but the calc's access summary carries the recipients it inherits); omitted when
        /// none. Headers use normal Swedish casing (from <see cref="AccessSummaryFormatter"/>).
        /// </summary>
        public static List<FilterOptionGroup> BuildGroups(IEnumerable<ListCalculationMVVM> calculations)
        {
            static FilterOption Opt(string s) => new() { Value = s, Label = s };

            var recipients = calculations
                .Where(c => c.Access is not null)
                .SelectMany(c => c.Access!.Recipients)
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
                    // Åtkomsttyp – endast huvudstatusarna. "Ej via projekt" är medvetet inte med här.
                    Header = AccessSummaryFormatter.GroupAccessType,
                    Options =
                    [
                        Opt(AccessSummaryFormatter.CalcTypePrivate),
                        Opt(AccessSummaryFormatter.CalcTypeViaProject),
                        Opt(AccessSummaryFormatter.CalcTypeDepartmentAccess),
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

        /// <summary>The values a calculation matches within a given filter group.</summary>
        public static IReadOnlyCollection<string> ValuesForGroup(ListCalculationMVVM c, string header)
        {
            if (header == AccessSummaryFormatter.GroupAccessType)
                return AccessSummaryFormatter.CalcTags(c.Access, c.IsPrivate).ToList();

            var recipients = c.Access?.Recipients;
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
        /// True when <paramref name="c"/> satisfies the selection: within every group that has at least
        /// one selected value the calculation must match one of them (OR within), and every such group
        /// must be satisfied (AND across). Groups with no selected value do not constrain the result.
        /// </summary>
        public static bool Matches(ListCalculationMVVM c, IReadOnlyCollection<FilterOptionGroup> groups, ICollection<string> selected)
        {
            foreach (var group in groups)
            {
                var selectedInGroup = group.Options.Select(o => o.Value).Where(selected.Contains).ToList();
                if (selectedInGroup.Count == 0)
                    continue;

                var values = ValuesForGroup(c, group.Header);
                if (!selectedInGroup.Any(values.Contains))
                    return false;
            }

            return true;
        }
    }
}

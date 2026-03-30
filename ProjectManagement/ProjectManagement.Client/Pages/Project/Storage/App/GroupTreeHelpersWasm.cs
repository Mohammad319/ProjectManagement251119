using ProjectManagement.Client.Helper;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.DTO.ProjectAppStorage.Service;

namespace ProjectManagement.Client.Pages.Project.Storage.App
{
    public enum GroupKind { Choice, Resource, Numeric }

    public sealed class GroupCardWasm
    {
        public GroupKind Kind { get; init; }
        public int Order { get; init; }
        public OptionGroupDto? Choice { get; init; }
        public ResourceOptionGroupDto? Resource { get; init; }
        public NumericInputDto? Numeric { get; init; }
    }

    public static class GroupTreeHelpersWasm
    {
        public static IEnumerable<GroupCardWasm> RootCards(ProjectTaskDto task)
        {
            if (task is null)
                return Enumerable.Empty<GroupCardWasm>();

            return task.OptionGroups
                .Where(g => string.IsNullOrWhiteSpace(g.SectionKey))
                .Select(ToCard)
                .Concat(task.ResourceOptionGroups
                    .Where(g => string.IsNullOrWhiteSpace(g.SectionKey))
                    .Select(ToCard))
                .Concat(task.NumericInputs
                    .Where(g => string.IsNullOrWhiteSpace(g.SectionKey))
                    .Select(ToCard))
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Kind)
                .ThenBy(c => c.Choice?.DisplayName ?? c.Resource?.DisplayName ?? c.Numeric?.DisplayName ?? string.Empty);
        }

        public static IEnumerable<GroupCardWasm> GetGroupsByKeys(ProjectTaskDto task, IEnumerable<string> keys)
        {
            if (task is null || keys is null)
                return Enumerable.Empty<GroupCardWasm>();

            var hs = new HashSet<string>(
                keys.Where(k => !string.IsNullOrWhiteSpace(k)),
                StringComparer.OrdinalIgnoreCase);

            return task.OptionGroups
                .Where(x => !string.IsNullOrWhiteSpace(x.SectionKey) && hs.Contains(x.SectionKey))
                .Select(ToCard)
                .Concat(task.ResourceOptionGroups
                    .Where(x => !string.IsNullOrWhiteSpace(x.SectionKey) && hs.Contains(x.SectionKey))
                    .Select(ToCard))
                .Concat(task.NumericInputs
                    .Where(x => !string.IsNullOrWhiteSpace(x.SectionKey) && hs.Contains(x.SectionKey))
                    .Select(ToCard))
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Kind)
                .ThenBy(c => c.Choice?.DisplayName ?? c.Resource?.DisplayName ?? c.Numeric?.DisplayName ?? string.Empty);
        }

        public static IEnumerable<string> RevealKeysFromSelected(OptionGroupDto g, UserAnswers ans)
        {
            if (g?.Options is null)
                yield break;

            foreach (var o in g.Options)
            {
                if (!ans.SelectedChoiceOptionIds.Contains(o.Id))
                    continue;

                foreach (var k in (o.RevealedSectionKeys ?? Enumerable.Empty<string>())
                             .Where(s => !string.IsNullOrWhiteSpace(s))
                             .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    yield return k;
                }
            }
        }

        public static string RangeLabel(decimal? min, decimal? max, int maxFractionDigits = 2)
        {
            if (min.HasValue && max.HasValue) return $"[{Format(min.Value, maxFractionDigits)} .. {Format(max.Value, maxFractionDigits)}]";
            if (min.HasValue) return $">= {Format(min.Value, maxFractionDigits)}";
            if (max.HasValue) return $"<= {Format(max.Value, maxFractionDigits)}";
            return "(any)";
        }

        public static GroupCardWasm ToCard(OptionGroupDto g) => new() { Kind = GroupKind.Choice, Order = g.SortOrder, Choice = g };
        public static GroupCardWasm ToCard(ResourceOptionGroupDto g) => new() { Kind = GroupKind.Resource, Order = g.SortOrder, Resource = g };
        public static GroupCardWasm ToCard(NumericInputDto g) => new() { Kind = GroupKind.Numeric, Order = g.SortOrder, Numeric = g };

        private static string Format(decimal value, int maxFractionDigits)
            => NumericFormatHelper.Format(value, maxFractionDigits);
    }
}

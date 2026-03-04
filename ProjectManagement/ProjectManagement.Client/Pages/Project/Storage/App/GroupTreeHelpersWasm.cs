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
            var rootChoices = task.OptionGroups.Where(g => string.IsNullOrWhiteSpace(g.SectionKey))
                .OrderBy(g => g.SortOrder).Select(ToCard);
            var rootResources = task.ResourceOptionGroups.Where(g => string.IsNullOrWhiteSpace(g.SectionKey))
                .OrderBy(g => g.SortOrder).Select(ToCard);
            var rootNumerics = task.NumericInputs.Where(g => string.IsNullOrWhiteSpace(g.SectionKey))
                .OrderBy(g => g.SortOrder).Select(ToCard);

            return rootChoices.Concat(rootResources).Concat(rootNumerics);
        }

        public static IEnumerable<GroupCardWasm> GetGroupsByKeys(ProjectTaskDto task, IEnumerable<string> keys)
        {
            if (keys is null) return Enumerable.Empty<GroupCardWasm>();
            var hs = new HashSet<string>(keys.Where(k => !string.IsNullOrWhiteSpace(k)), StringComparer.OrdinalIgnoreCase);
            var list = new List<GroupCardWasm>();

            foreach (var g in task.OptionGroups.Where(x => !string.IsNullOrWhiteSpace(x.SectionKey) && hs.Contains(x.SectionKey))) list.Add(ToCard(g));
            foreach (var g in task.ResourceOptionGroups.Where(x => !string.IsNullOrWhiteSpace(x.SectionKey) && hs.Contains(x.SectionKey))) list.Add(ToCard(g));
            foreach (var g in task.NumericInputs.Where(x => !string.IsNullOrWhiteSpace(x.SectionKey) && hs.Contains(x.SectionKey))) list.Add(ToCard(g));

            return list.OrderBy(c => c.Order);
        }

        public static IEnumerable<string> RevealKeysFromSelected(OptionGroupDto g, UserAnswers ans)
        {
            if (g?.Options is null) yield break;
            foreach (var o in g.Options)
            {
                if (ans.SelectedChoiceOptionIds.Contains(o.Id))
                {
                    foreach (var k in (o.RevealedSectionKeys ?? Enumerable.Empty<string>())
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Distinct(StringComparer.OrdinalIgnoreCase))
                        yield return k;
                }
            }
        }

        public static string RangeLabel(decimal? min, decimal? max)
        {
            if (min.HasValue && max.HasValue) return $"[{min} .. {max}]";
            if (min.HasValue) return $">= {min}";
            if (max.HasValue) return $"<= {max}";
            return "(any)";
        }

        public static GroupCardWasm ToCard(OptionGroupDto g) => new() { Kind = GroupKind.Choice, Order = g.SortOrder, Choice = g };
        public static GroupCardWasm ToCard(ResourceOptionGroupDto g) => new() { Kind = GroupKind.Resource, Order = g.SortOrder, Resource = g };
        public static GroupCardWasm ToCard(NumericInputDto g) => new() { Kind = GroupKind.Numeric, Order = g.SortOrder, Numeric = g };
    }

}

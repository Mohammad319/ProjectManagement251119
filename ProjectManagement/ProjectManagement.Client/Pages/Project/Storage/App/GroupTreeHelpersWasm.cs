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
            => Enumerable.Empty<GroupCardWasm>();

        public static IEnumerable<GroupCardWasm> GetGroupsByKeys(ProjectTaskDto task, IEnumerable<string> keys)
            => Enumerable.Empty<GroupCardWasm>();

        public static IEnumerable<string> RevealKeysFromSelected(OptionGroupDto g, UserAnswers ans)
            => Enumerable.Empty<string>();

        public static string RangeLabel(decimal? min, decimal? max, int maxFractionDigits = 2)
        {
            if (min.HasValue && max.HasValue) return $"[{min.Value:G} .. {max.Value:G}]";
            if (min.HasValue) return $">= {min.Value:G}";
            if (max.HasValue) return $"<= {max.Value:G}";
            return "(any)";
        }
    }
}

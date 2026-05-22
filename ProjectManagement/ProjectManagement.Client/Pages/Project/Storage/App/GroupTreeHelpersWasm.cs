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
            if (min.HasValue && max.HasValue)
                return $"[{NumericFormatHelper.Format(min.Value, maxFractionDigits)} .. {NumericFormatHelper.Format(max.Value, maxFractionDigits)}]";

            if (min.HasValue)
                return $">= {NumericFormatHelper.Format(min.Value, maxFractionDigits)}";

            if (max.HasValue)
                return $"<= {NumericFormatHelper.Format(max.Value, maxFractionDigits)}";

            return "(any)";
        }
    }
}

using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Constants;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace ProjectManagement.Client.Shared.Calculation
{
    public static class CalculationTableResizeHelper
    {
        public static bool TryParseResizePayload(string payload, out int headerIndex, out int width)
        {
            headerIndex = 0;
            width = 0;

            if (string.IsNullOrWhiteSpace(payload))
                return false;

            string[] parts = payload.Split("||", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return false;

            if (!int.TryParse(parts[0], out headerIndex) || !int.TryParse(parts[1], out width))
                return false;

            width = Math.Max(PMValuesConst.MinWidthCol, width);
            return true;
        }

        public static bool TryResolveColumnId(
            int headerIndex,
            IReadOnlyList<NetColumnId> visibleColumnIds,
            out NetColumnId columnId)
        {
            columnId = default;

            int visibleIndex = headerIndex - 2;
            if (visibleIndex < 0 || visibleIndex >= visibleColumnIds.Count)
                return false;

            columnId = visibleColumnIds[visibleIndex];
            return true;
        }
    }
}

using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Constants;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Domain.Entities.Calculation
{
    internal static class TemplateMetadataMapper
    {
        public static TemplateData Build(TemplateData? metadata)
        {
            metadata ??= new TemplateData();

            return new TemplateData
            {
                MathRound = metadata.MathRound,
                Currency = Normalize(metadata.Currency, 5, "€"),
                DateFormat = Normalize(metadata.DateFormat, 15, "dd.MM.yyyy"),
                NetCalc = CloneNetCalc(metadata.NetCalc),
                SummarySheet = CloneSummarySheet(metadata.SummarySheet)
            };
        }

        private static NetCalc CloneNetCalc(NetCalc? value)
        {
            value ??= new NetCalc();

            return new NetCalc
            {
                Color = CloneNetColor(value.Color),
                Columns = value.Columns?.Select(CloneNetColumn).ToList() ?? TemplateDefaults.NetCalc()
            };
        }

        private static SummarySheet CloneSummarySheet(SummarySheet? value)
        {
            value ??= new SummarySheet();

            return new SummarySheet
            {
                Color = CloneSummarySheetColor(value.Color),
                Columns = value.Columns?.Select(CloneSummaryColumn).ToList() ?? TemplateDefaults.SummarySheet()
            };
        }

        private static NetColor CloneNetColor(NetColor? value)
        {
            value ??= new NetColor();

            return new NetColor
            {
                Header = value.Header ?? "#e1f0ad",
                Note = value.Note ?? "#f0ebeb",
                Border = value.Border ?? "#000",
                BorderStyle = value.BorderStyle ?? "dotted",
                Text = value.Text ?? "#000",
                Task = value.Task ?? TemplateConstBase.Task,
                SubTask = value.SubTask ?? TemplateConstBase.SubTask,
                Resource = value.Resource ?? TemplateConstBase.Resource
            };
        }

        private static SummarySheetColor CloneSummarySheetColor(SummarySheetColor? value)
        {
            value ??= new SummarySheetColor();

            return new SummarySheetColor
            {
                Header = value.Header ?? "#e1f0ad",
                Note = value.Note ?? "#f0ebeb",
                Border = value.Border ?? "#000",
                BorderStyle = value.BorderStyle ?? "dotted",
                Text = value.Text ?? "#000",
                Sum = value.Sum ?? "#c4c7fe",
                Factor = value.Factor ?? "#c4c7fe"
            };
        }

        private static NetColumnState CloneNetColumn(NetColumnState value)
            => new()
            {
                Id = value.Id,
                Width = value.Width,
                Frozen = value.Frozen,
                StartPX = value.StartPX
            };

        private static SummarySheetColumnState CloneSummaryColumn(SummarySheetColumnState value)
            => new()
            {
                Id = value.Id,
                Width = value.Width,
                Frozen = value.Frozen
            };

        private static string Normalize(string? value, int maxLength, string fallback)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
        }
    }
}

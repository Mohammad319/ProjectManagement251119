using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Constants;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Domain.Entities.Calculation
{
    internal static class TemplateMetadataMapper
    {
        public static TemplateMetadataData Build(TemplateData? metadata)
        {
            metadata ??= new TemplateData();

            return new TemplateMetadataData
            {
                MathRound = metadata.MathRound,
                Currency = Normalize(metadata.Currency, 5, "â‚¬"),
                DateFormat = Normalize(metadata.DateFormat, 15, "dd.MM.yyyy"),
                NetCalc = CloneNetCalcStyle(metadata.NetCalc),
                SummarySheet = CloneSummarySheet(metadata.SummarySheet)
            };
        }

        public static TemplateMetadataData Build(TemplateMetadataData? metadata)
        {
            metadata ??= new TemplateMetadataData();

            return new TemplateMetadataData
            {
                MathRound = metadata.MathRound,
                Currency = Normalize(metadata.Currency, 5, "â‚¬"),
                DateFormat = Normalize(metadata.DateFormat, 15, "dd.MM.yyyy"),
                NetCalc = CloneNetCalcStyle(metadata.NetCalc),
                SummarySheet = CloneSummarySheet(metadata.SummarySheet)
            };
        }

        public static TemplateData BuildTemplateData(
            TemplateMetadataData? metadata,
            IEnumerable<NetColumnState>? columns)
        {
            var normalized = Build(metadata);

            return new TemplateData
            {
                MathRound = normalized.MathRound,
                Currency = normalized.Currency,
                DateFormat = normalized.DateFormat,
                NetCalc = new NetCalc
                {
                    Color = CloneNetColor(normalized.NetCalc.Color),
                    Columns = TemplateDefaults.EnsureNetCalcColumns(columns),
                    Sort = new SortConfig()
                },
                SummarySheet = CloneSummarySheet(normalized.SummarySheet)
            };
        }

        private static NetCalcStyleData CloneNetCalcStyle(NetCalc? value)
        {
            value ??= new NetCalc();

            return new NetCalcStyleData
            {
                Color = CloneNetColor(value.Color)
            };
        }

        private static NetCalcStyleData CloneNetCalcStyle(NetCalcStyleData? value)
        {
            value ??= new NetCalcStyleData();

            return new NetCalcStyleData
            {
                Color = CloneNetColor(value.Color)
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
                Resource = value.Resource ?? TemplateConstBase.Resource,
                TaskCodeName = value.TaskCodeName ?? TemplateConstBase.TaskCodeName,
                TaskDetailBaseQuantity = value.TaskDetailBaseQuantity ?? TemplateConstBase.TaskDetailBaseQuantity,
                ResourceParameter = value.ResourceParameter ?? TemplateConstBase.ResourceParameter,
                ResourceAttachment = value.ResourceAttachment ?? TemplateConstBase.ResourceAttachment,
                ResourceTime = value.ResourceTime ?? TemplateConstBase.ResourceTime
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

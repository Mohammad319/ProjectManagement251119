using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Constants;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.Calculation.Template
{
    public class NetColor
    {
        public string Header { get; set; } = "#e1f0ad";
        public string Note { get; set; } = "#f0ebeb";
        public string Border { get; set; } = "#000";
        public string BorderStyle { get; set; } = "dotted";
        public string Text { get; set; } = "#000";
        public string Task { get; set; } = TemplateConstBase.Task;
        public string SubTask { get; set; } = TemplateConstBase.SubTask;
        public string Resource { get; set; } = TemplateConstBase.Resource;

        public NetColor Clone()
        {
            return new NetColor
            {
                Header = Header ?? "#e1f0ad",
                Note = Note ?? "#f0ebeb",
                Border = Border ?? "#000",
                BorderStyle = BorderStyle ?? "dotted",
                Text = Text ?? "#000",
                Task = Task ?? TemplateConstBase.Task,
                SubTask = SubTask ?? TemplateConstBase.SubTask,
                Resource = Resource ?? TemplateConstBase.Resource
            };
        }
    }

    public class SummarySheetColor
    {
        public string Header { get; set; } = "#e1f0ad";
        public string Note { get; set; } = "#f0ebeb";
        public string Border { get; set; } = "#000";
        public string BorderStyle { get; set; } = "dotted";
        public string Text { get; set; } = "#000";
        public string Sum { get; set; } = "#c4c7fe";
        public string Factor { get; set; } = "#c4c7fe";

        public SummarySheetColor Clone()
        {
            return new SummarySheetColor
            {
                Header = Header ?? "#e1f0ad",
                Note = Note ?? "#f0ebeb",
                Border = Border ?? "#000",
                BorderStyle = BorderStyle ?? "dotted",
                Text = Text ?? "#000",
                Sum = Sum ?? "#c4c7fe",
                Factor = Factor ?? "#c4c7fe"
            };
        }
    }

    public class NetCalc
    {
        public NetColor Color { get; set; } = new();
        public List<NetColumnState> Columns { get; set; } = TemplateDefaults.NetCalc();

        public NetCalc Clone()
        {
            return new NetCalc
            {
                Color = (Color ?? new NetColor()).Clone(),
                Columns = Columns?.Select(CloneNetColumn).ToList() ?? TemplateDefaults.NetCalc()
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
    }

    public class SummarySheet
    {
        public SummarySheetColor Color { get; set; } = new();
        public List<SummarySheetColumnState> Columns { get; set; } = TemplateDefaults.SummarySheet();

        public SummarySheet Clone()
        {
            return new SummarySheet
            {
                Color = (Color ?? new SummarySheetColor()).Clone(),
                Columns = Columns?.Select(CloneSummaryColumn).ToList() ?? TemplateDefaults.SummarySheet()
            };
        }

        private static SummarySheetColumnState CloneSummaryColumn(SummarySheetColumnState value)
            => new()
            {
                Id = value.Id,
                Width = value.Width,
                Frozen = value.Frozen
            };
    }

    public class TemplateData
    {
        [JsonIgnore]
        public int MathRound { get; set; } = 2;

        [MaxLength(5)]
        public string Currency { get; set; } = "€";

        [MaxLength(15)]
        public string DateFormat { get; set; } = "dd.MM.yyyy";

        public NetCalc NetCalc { get; set; } = new();
        public SummarySheet SummarySheet { get; set; } = new();

        public TemplateData Clone()
        {
            return new TemplateData
            {
                MathRound = MathRound,
                Currency = Normalize(Currency, "€"),
                DateFormat = Normalize(DateFormat, "dd.MM.yyyy"),
                NetCalc = (NetCalc ?? new NetCalc()).Clone(),
                SummarySheet = (SummarySheet ?? new SummarySheet()).Clone()
            };
        }

        private static string Normalize(string? value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    public class TemplateBaseData : TemplateBase
    {
        private TemplateData? data = new();

        [JsonIgnore]
        public TemplateData Data
        {
            get
            {
                data ??= new TemplateData();
                return data;
            }
            set => data = value?.Clone() ?? new TemplateData();
        }

        [JsonIgnore]
        public int MathRound
        {
            get => Data.MathRound;
            set => Data.MathRound = value;
        }

        [MaxLength(5)]
        public string Currency
        {
            get => Data.Currency;
            set => Data.Currency = string.IsNullOrWhiteSpace(value) ? "€" : value.Trim();
        }

        [MaxLength(15)]
        public string DateFormat
        {
            get => Data.DateFormat;
            set => Data.DateFormat = string.IsNullOrWhiteSpace(value) ? "dd.MM.yyyy" : value.Trim();
        }

        public NetCalc NetCalc
        {
            get => Data.NetCalc;
            set => Data.NetCalc = value?.Clone() ?? new NetCalc();
        }

        public SummarySheet SummarySheet
        {
            get => Data.SummarySheet;
            set => Data.SummarySheet = value?.Clone() ?? new SummarySheet();
        }
    }

    public class TemplateListPostDTO : TemplateBaseData
    {
        [Range(0, 200, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public bool Active { get; set; } = true;
    }

    public class TemplateModelDTO : TemplateBaseData
    {
        public int Id { get; set; }
    }

    public class TemplateListDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? TemplateId { get; set; }
    }
}

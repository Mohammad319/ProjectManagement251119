using ProjectManagement.Shared.Base.Application;
using System.Text.Json;

namespace ProjectManagement.Shared.DTO.App;

public static class SelfInspectionStandardTemplates
{
    public const string ChecklistKey = "system-checklista-kalkylloppet";
    public const string HandoverKey = "system-overlamning-kalkyl-produktion";
    public const string RiskAnalysisKey = "system-riskanalys";

    private static readonly string[] RiskCategories =
    [
        "Ekonomi",
        "Tid",
        "Produktion",
        "Arbetsmiljö",
        "Miljö",
        "Kvalitet",
        "Inköp",
        "Kund / avtal",
        "Resurs",
        "Annat"
    ];

    public static IReadOnlyList<ApplicationDTO> CreateSystemTemplates(int departmentId)
    {
        if (departmentId <= 0)
            return [];

        return
        [
            CreateChecklist(departmentId, true),
            CreateHandover(departmentId, true),
            CreateRiskAnalysis(departmentId, true)
        ];
    }

    public static ApplicationDTO CreateCopy(ApplicationDTO source)
    {
        var copy = CloneTemplate(source);
        copy.Id = 0;
        copy.Name = $"{source.Name} - kopia";
        copy.Data.IsSystemTemplate = false;
        copy.Data.CopiedFromSystemTemplateKey = source.Data.SystemTemplateKey;
        copy.Data.SystemTemplateKey = string.Empty;
        ResetIds(copy);
        return copy;
    }

    public static ApplicationDTO CreateChecklist(int departmentId, bool systemTemplate = false)
    {
        var sections = new (string Section, string[] Points)[]
        {
            ("Förfrågan", [
                "Bevaka förfrågningsunderlag",
                "Kontrollera skallkrav",
                "Kontrollera referenskrav",
                "Kontrollera kundens betalningsförmåga"
            ]),
            ("Vid projektering", [
                "Upprätta produktionstidsplan",
                "Genomgång av AF-delen"
            ]),
            ("Kalkyl", [
                "Riskhantering i riskanalys",
                "Kalkylera",
                "Färdig kalkyl sparas som PDF"
            ]),
            ("Anbudslämnande", [
                "Upprätta och kontrollera anbudsbrev",
                "Skicka anbudet"
            ]),
            ("Vunnet anbud", [
                "Bekräfta vunnet anbud"
            ]),
            ("Överlämning till produktion", [
                "Överlämning till produktion",
                "Kalkylgenomgång med PC"
            ]),
            ("Avslut", [
                "Stäng och arkivera kalkylunderlag"
            ])
        };

        return CreateTemplate(
            departmentId,
            "Checklista - Kalkylloppet",
            SelfInspectionTemplateTypes.Checklist,
            "Kontrollera viktiga steg från förfrågan till anbud, överlämning och avslut.",
            systemTemplate ? ChecklistKey : string.Empty,
            systemTemplate,
            sections.SelectMany((x, sectionIndex) =>
            {
                var section = CreateSection(x.Section, sectionIndex);
                return x.Points.Select((point, pointIndex) => CreateRow(point, section, pointIndex, ChecklistColumns()));
            }).ToList());
    }

    public static ApplicationDTO CreateHandover(int departmentId, bool systemTemplate = false)
    {
        var sections = new[]
        {
            "Beskrivning av objekt",
            "Förfrågningsunderlag och avtal",
            "Organisation och resursplanering",
            "Tidplan och produktionsplanering",
            "Inköp, offerter och leverantörer",
            "Kalkylunderlag",
            "Kvalitet, miljö och arbetsmiljö",
            "Riskanalys",
            "Ekonomi",
            "Övrigt",
            "Nästa möte"
        };

        return CreateTemplate(
            departmentId,
            "Överlämning Kalkyl - Produktion",
            SelfInspectionTemplateTypes.Handover,
            "Stöd för överlämningsmöte mellan kalkyl och produktion.",
            systemTemplate ? HandoverKey : string.Empty,
            systemTemplate,
            sections.Select((sectionTitle, index) =>
            {
                var section = CreateSection(sectionTitle, index);
                return CreateRow(sectionTitle, section, index, HandoverColumns());
            }).ToList());
    }

    public static ApplicationDTO CreateRiskAnalysis(int departmentId, bool systemTemplate = false)
    {
        return CreateTemplate(
            departmentId,
            "Riskanalys",
            SelfInspectionTemplateTypes.RiskAnalysis,
            "Identifiera och följa upp risker innan anbud eller produktion.",
            systemTemplate ? RiskAnalysisKey : string.Empty,
            systemTemplate,
            [
                CreateRow("Riskrad", CreateSection("Riskanalys", 0), 0, RiskAnalysisColumns())
            ]);
    }

    public static IReadOnlyList<string> GetRiskCategories() => RiskCategories;

    private static ApplicationDTO CreateTemplate(
        int departmentId,
        string name,
        string type,
        string purpose,
        string systemTemplateKey,
        bool systemTemplate,
        List<RowDTO> rows)
    {
        return new ApplicationDTO
        {
            DepartmentId = departmentId,
            Name = name,
            IsVisible = true,
            Data = new ApplicationDataDTO
            {
                Description = purpose,
                Purpose = purpose,
                TemplateType = type,
                IsSystemTemplate = systemTemplate,
                SystemTemplateKey = systemTemplateKey,
                Sections = CreateSectionsFromRows(rows),
                Rows = rows
            }
        };
    }

    private static List<SelfInspectionSectionData> CreateSectionsFromRows(List<RowDTO> rows)
    {
        var sections = new List<SelfInspectionSectionData>();
        foreach (var row in rows)
        {
            if (sections.Any(x => x.Id == row.SectionId))
                continue;

            sections.Add(new SelfInspectionSectionData
            {
                Id = row.SectionId,
                Title = row.SectionTitle,
                SortOrder = sections.Count + 1,
                IsVisible = true
            });
        }

        return sections;
    }

    private static SelfInspectionSectionData CreateSection(string title, int sortOrder)
    {
        return new SelfInspectionSectionData
        {
            Id = Guid.NewGuid(),
            Title = title,
            SortOrder = sortOrder,
            IsVisible = true
        };
    }

    private static RowDTO CreateRow(string name, SelfInspectionSectionData section, int sortOrder, IReadOnlyList<AttributeDTO> columns)
    {
        return new RowDTO
        {
            ID = Guid.NewGuid(),
            Name = name,
            Description = section.Title,
            SectionId = section.Id,
            SectionTitle = section.Title,
            SortOrder = sortOrder,
            IsVisible = true,
            Attributes = columns.Select(CloneColumn).ToList()
        };
    }

    private static IReadOnlyList<AttributeDTO> ChecklistColumns() =>
    [
        Column("Notering", AttributeType.TextArea, false),
        Column("Ansvarig", AttributeType.Text, false),
        Column("Datum", AttributeType.Date, false),
        Column("Klart", AttributeType.Bool, false)
    ];

    private static IReadOnlyList<AttributeDTO> HandoverColumns() =>
    [
        Column("Ansvarig", AttributeType.Text, false),
        Column("Klart", AttributeType.Bool, false),
        Column("Kommentar", AttributeType.TextArea, false)
    ];

    private static IReadOnlyList<AttributeDTO> RiskAnalysisColumns() =>
    [
        Column("Risk", AttributeType.TextArea, true, RiskAnalysisFieldKeys.Risk),
        Column("Konsekvens", AttributeType.TextArea, false, RiskAnalysisFieldKeys.Consequence),
        Column("Kategori", AttributeType.Select, false, RiskAnalysisFieldKeys.Category, JsonSerializer.Serialize(new { Values = RiskCategories, DefaultIndex = 0 })),
        Column("Sannolikhet", AttributeType.Int, true, RiskAnalysisFieldKeys.Probability, NumberValidation()),
        Column("Konsekvensvärde", AttributeType.Int, true, RiskAnalysisFieldKeys.Impact, NumberValidation()),
        Column("Riskvärde", AttributeType.Int, false, RiskAnalysisFieldKeys.RiskValue),
        Column("Risknivå", AttributeType.Text, false, RiskAnalysisFieldKeys.RiskLevel),
        Column("Riskägare", AttributeType.Text, false, RiskAnalysisFieldKeys.RiskOwner),
        Column("Riskaktivitet", AttributeType.TextArea, false, RiskAnalysisFieldKeys.RiskActivity),
        Column("Ansvarig", AttributeType.Text, false, RiskAnalysisFieldKeys.Responsible),
        Column("Uppföljningsdatum", AttributeType.Date, false, RiskAnalysisFieldKeys.FollowUpDate),
        Column("Kommentar", AttributeType.TextArea, false, RiskAnalysisFieldKeys.Comment),
        Column("Klart", AttributeType.Bool, false, RiskAnalysisFieldKeys.Done)
    ];

    private static string NumberValidation() => JsonSerializer.Serialize(new { Min = 1, Max = 5, Default = 1 });

    private static AttributeDTO Column(string label, AttributeType type, bool required, string key = "", string validation = "")
    {
        return new AttributeDTO
        {
            ID = Guid.NewGuid(),
            AttributeType = type,
            Required = required,
            Validation = validation,
            Label = label,
            FieldKey = key,
            FieldTypeLabel = ToFieldTypeLabel(type),
            IsComputed = key is RiskAnalysisFieldKeys.RiskValue or RiskAnalysisFieldKeys.RiskLevel,
            Style = string.IsNullOrWhiteSpace(key)
                ? $"label:{label}"
                : $"key:{key};label:{label}"
        };
    }

    private static string ToFieldTypeLabel(AttributeType type) => type switch
    {
        AttributeType.TextArea => "Lång text",
        AttributeType.Int or AttributeType.Double => "Tal",
        AttributeType.Date => "Datum",
        AttributeType.DateTime => "Datum och tid",
        AttributeType.Bool => "Checkbox",
        AttributeType.Select => "Dropdown",
        _ => "Text"
    };

    private static AttributeDTO CloneColumn(AttributeDTO source)
    {
        var clone = source.Clone();
        clone.ID = Guid.NewGuid();
        return clone;
    }

    private static ApplicationDTO CloneTemplate(ApplicationDTO source)
    {
        return new ApplicationDTO
        {
            Id = source.Id,
            DepartmentId = source.DepartmentId,
            Name = source.Name,
            IsVisible = source.IsVisible,
            UserId = source.UserId,
            LastUpdate = source.LastUpdate,
            Data = source.Data.Clone()
        };
    }

    private static void ResetIds(ApplicationDTO template)
    {
        var sectionIdMap = template.Data.Sections.ToDictionary(x => x.Id, _ => Guid.NewGuid());
        foreach (var section in template.Data.Sections)
            section.Id = sectionIdMap[section.Id];

        foreach (var row in template.Data.Rows)
        {
            row.ID = Guid.NewGuid();
            if (sectionIdMap.TryGetValue(row.SectionId, out var newSectionId))
                row.SectionId = newSectionId;
            foreach (var attr in row.Attributes)
                attr.ID = Guid.NewGuid();
        }
    }
}

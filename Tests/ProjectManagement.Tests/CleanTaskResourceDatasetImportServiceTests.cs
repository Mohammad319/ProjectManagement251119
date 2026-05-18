using System.Text;
using ProjectManagement.Shared.Enums;
using TaskResourceBlueprints.Services.Import;
using Xunit;

namespace ProjectManagement.Tests;

public class CleanTaskResourceDatasetImportServiceTests
{
    [Fact]
    public async Task Reader_ParsesUnifiedCleanDataColumnsForTasksOnlyAndResources()
    {
        await using var stream = TextStream("""
RowType;Code;ParentCode;Name;Quantity;Unit;TaskNameSynonym1;TaskNameSynonym2;TaskUnitSynonym1;TaskUnitSynonym2;ResourceType;UnitCost
T;BBB.131;;Geotekniska forhallanden i jord;1;belopp;Geoteknisk undersokning;Jordmekanik;projektenhet;egendefinierad enhet;;
R;;BBB.131;Anlaggare;1;h;;;;;Worker;610
R;;BBB.131;Servicebil;1;h;;;;;Machine;210
T;BBB.132;;Geotekniska forhallanden i berg;1;belopp;Bergteknik;Bergundersokning;projektenhet;egendefinierad enhet;;
""");

        var (analysis, tasks, links) = await CleanTaskResourceDatasetReader.ReadAsync(stream, "unified.csv");

        Assert.Equal(2, analysis.UniqueTasks);
        Assert.Equal(2, analysis.Links);
        Assert.Equal(2, tasks.Count);
        Assert.Equal(2, links.Count);
        Assert.Equal("Geotekniska forhallanden i jord", tasks[0].Name);
        Assert.Equal(["Geoteknisk undersokning", "Jordmekanik"], tasks[0].NameSynonyms);
        Assert.Equal(["projektenhet", "egendefinierad enhet"], tasks[0].UnitSynonyms);
        Assert.Equal("BBB.131", links[0].Task.Code);
        Assert.Equal("Anlaggare", links[0].ResourceName);
        Assert.Equal(ResourceTypesEnum.Worker, links[0].ResourceType);
        Assert.Equal(610m, links[0].UnitCost);
    }

    [Fact]
    public async Task Reader_ParsesUnifiedCleanDataTaskRowsWithoutResources()
    {
        await using var stream = TextStream("""
RowType;Code;ParentCode;Name;Unit;TaskNameSynonym1;TaskNameSynonym2;TaskUnitSynonym1;TaskUnitSynonym2;ResourceType;UnitCost
T;CBB.311;;Schakt for ledning;m3;Gravning ledning;Ledningsschakt;kubikmeter;kbm;;
T;HUS.100;;Malning av vagg;m2;Vaggmalning;Mala vagg;kvadratmeter;kvm;;
""");

        var (analysis, tasks, links) = await CleanTaskResourceDatasetReader.ReadAsync(stream, "task-only-unified.csv");

        Assert.Equal(2, analysis.UniqueTasks);
        Assert.Equal(0, analysis.Links);
        Assert.Equal(2, tasks.Count);
        Assert.Empty(links);
        Assert.Equal("CBB.311", tasks[0].Code);
        Assert.Null(tasks[0].ParentCode);
        Assert.Equal("m3", tasks[0].UnitCode);
        Assert.Null(tasks[0].Quantity);
    }

    [Fact]
    public async Task Reader_AcceptsResourceTypeEnumNames()
    {
        await using var stream = TextStream("""
RowType;Code;ParentCode;Name;Unit;ResourceType;UnitCost
T;GEN.001;;General information task;st;;
R;;GEN.001;Information row;st;Information;0
R;;GEN.001;Project overhead row;st;ProjectOverheadCosts;10
R;;GEN.001;Adjustment row;st;Adjustment;5
""");

        var (analysis, _, links) = await CleanTaskResourceDatasetReader.ReadAsync(stream, "enum-types.csv");

        Assert.Equal(3, analysis.Links);
        Assert.Equal(ResourceTypesEnum.Information, links[0].ResourceType);
        Assert.Equal(ResourceTypesEnum.ProjectOverheadCosts, links[1].ResourceType);
        Assert.Equal(ResourceTypesEnum.Adjustment, links[2].ResourceType);
    }

    [Fact]
    public async Task Reader_ParsesHierarchicalTaskRowsWithResourcesBelow()
    {
        await using var stream = TextStream("""
RowType;TaskCode;TaskName;TaskQuantity;TaskUnit;TaskNameSynonym1;TaskNameSynonym2;TaskUnitSynonym1;TaskUnitSynonym2;ResourceName;ResourceType;ResourceFolder;ResourceQuantity;ResourceUnit;UnitCost
T;CBB.311;Schakt for ledning;12.5;m3;Gravning ledning;Ledningsschakt;kubikmeter;kbm;;;;;;
R;;;;;;;;;Gravmaskin 14 ton;Machine;Maskiner;1;h;650
R;;;;;;;;;Lastbil transport;Machine;Transport;1;h;850
T;HUS.100;Malning av vagg;20;m2;;;;;;;;;;
R;;;;;;;;;Malarfarg vit;Material;Material/Farg;0.2;l;120
""");

        var (analysis, tasks, links) = await CleanTaskResourceDatasetReader.ReadAsync(stream, "tasks-resources.csv");

        Assert.Equal(2, analysis.UniqueTasks);
        Assert.Equal(3, analysis.Links);
        Assert.Equal(3, analysis.UniqueResources);
        Assert.Equal(2, tasks.Count);
        Assert.Equal(3, links.Count);
        Assert.Equal(ResourceTypesEnum.MachinesAndEquipments, links[0].ResourceType);
        Assert.Equal("Schakt for ledning", links[0].Task.Name);
        Assert.Equal(["Gravning ledning", "Ledningsschakt"], links[0].Task.NameSynonyms);
        Assert.Equal(["kubikmeter", "kbm"], links[0].Task.UnitSynonyms);
        Assert.Equal("Malarfarg vit", links[2].ResourceName);
    }

    [Fact]
    public async Task Reader_ParsesFlatTaskResourceRows()
    {
        await using var stream = TextStream("""
TaskCode;TaskName;TaskQuantity;TaskUnit;ResourceName;ResourceType;ResourceFolder;ResourceQuantity;ResourceUnit;UnitCost
CBB.311;Schakt for ledning;12.5;m3;Gravmaskin 14 ton;Machine;Maskiner;1;h;650
CBB.311;Schakt for ledning;12.5;m3;Lastbil transport;Machine;Transport;1;h;850
""");

        var (analysis, tasks, links) = await CleanTaskResourceDatasetReader.ReadAsync(stream, "flat.csv");

        Assert.Equal(1, analysis.UniqueTasks);
        Assert.Equal(2, analysis.Links);
        Assert.Single(tasks);
        Assert.Equal("Lastbil transport", links[1].ResourceName);
    }

    [Fact]
    public async Task Reader_ParsesJsonTasksWithNestedResources()
    {
        await using var stream = TextStream("""
[
  {
    "taskCode": "CBB.311",
    "parentCode": "CBB",
    "taskName": "Schakt for ledning",
    "taskQuantity": 12.5,
    "taskUnit": "m3",
    "taskNameSynonyms": ["Gravning ledning"],
    "taskUnitSynonyms": ["kubikmeter"],
    "taskNameSynonym2": "Ledningsschakt",
    "taskUnitSynonym2": "kbm",
    "resources": [
      { "resourceName": "Gravmaskin 14 ton", "resourceType": "Machine", "resourceFolder": "Maskiner", "resourceQuantity": 1, "resourceUnit": "h", "unitCost": 650 },
      { "resourceName": "Lastbil transport", "resourceType": "Machine", "resourceFolder": "Transport", "resourceQuantity": 1, "resourceUnit": "h", "unitCost": 850 }
    ]
  }
]
""");

        var (analysis, tasks, links) = await CleanTaskResourceDatasetReader.ReadAsync(stream, "tasks-resources.json");

        Assert.Equal(1, analysis.UniqueTasks);
        Assert.Equal(2, analysis.Links);
        Assert.Single(tasks);
        Assert.Equal(2, links.Count);
        Assert.Equal(ResourceTypesEnum.MachinesAndEquipments, links[0].ResourceType);
        Assert.Equal(650m, links[0].UnitCost);
        Assert.Equal("CBB", tasks[0].ParentCode);
        Assert.Contains("Gravning ledning", tasks[0].NameSynonyms);
        Assert.Contains("Ledningsschakt", tasks[0].NameSynonyms);
        Assert.Contains("kubikmeter", tasks[0].UnitSynonyms);
        Assert.Contains("kbm", tasks[0].UnitSynonyms);
    }

    private static MemoryStream TextStream(string content)
        => new(Encoding.UTF8.GetBytes(content.ReplaceLineEndings("\n")));
}

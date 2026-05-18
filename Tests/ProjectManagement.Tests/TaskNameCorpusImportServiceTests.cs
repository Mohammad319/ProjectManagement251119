using System.Text;
using ClosedXML.Excel;
using TaskResourceBlueprints.Services.Import;
using Xunit;

namespace ProjectManagement.Tests;

public class TaskNameCorpusImportServiceTests
{
    [Fact]
    public async Task Reader_ParsesOneTaskPerLineAndSkipsDuplicates()
    {
        await using var stream = TextStream("""
Schakt för ledning
Målning av vägg
Schakt för ledning
""");

        var (analysis, rows) = await TaskNameCorpusFileReader.ReadAsync(stream, "tasks.txt");

        Assert.Equal(3, analysis.TotalRows);
        Assert.Equal(2, analysis.UniqueTasks);
        Assert.Equal(1, analysis.DuplicateRows);
        Assert.Equal(["Schakt för ledning", "Målning av vägg"], rows.Select(x => x.Name));
    }

    [Fact]
    public async Task Reader_DetectsCsvColumnsFromHeaders()
    {
        await using var stream = TextStream("""
Kod;ParentCode;Namn;Mängd;Enhet;TaskNameSynonym1;TaskNameSynonym2;TaskUnitSynonym1;TaskUnitSynonym2
CBB.311;CBB;Schakt för ledning;12,5;m3;Gravning ledning;Ledningsschakt;kubikmeter;kbm
HUS.100;HUS;Målning av vägg;20;m2;;;;
""");

        var (analysis, rows) = await TaskNameCorpusFileReader.ReadAsync(stream, "tasks.csv");

        Assert.Equal("Namn", analysis.NameColumn);
        Assert.Equal("Kod", analysis.CodeColumn);
        Assert.Equal("Mängd", analysis.QuantityColumn);
        Assert.Equal("Enhet", analysis.UnitColumn);
        Assert.Equal(2, analysis.UniqueTasks);
        Assert.Equal("CBB.311", rows[0].Code);
        Assert.Equal("CBB", rows[0].ParentCode);
        Assert.Equal(12.5m, rows[0].Quantity);
        Assert.Equal("m3", rows[0].UnitCode);
        Assert.Equal(["Gravning ledning", "Ledningsschakt"], rows[0].NameSynonyms);
        Assert.Equal(["kubikmeter", "kbm"], rows[0].UnitSynonyms);
    }

    [Fact]
    public async Task Reader_ParsesExcelTaskNameColumn()
    {
        await using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Tasks");
            sheet.Cell(1, 1).Value = "TaskName";
            sheet.Cell(2, 1).Value = "Schakt för ledning";
            sheet.Cell(3, 1).Value = "Målning av vägg";
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        var (analysis, rows) = await TaskNameCorpusFileReader.ReadAsync(stream, "tasks.xlsx");

        Assert.Equal(2, analysis.UniqueTasks);
        Assert.Equal("TaskName", analysis.NameColumn);
        Assert.Equal("Schakt för ledning", rows[0].Name);
    }

    private static MemoryStream TextStream(string content)
        => new(Encoding.UTF8.GetBytes(content.ReplaceLineEndings("\n")));
}

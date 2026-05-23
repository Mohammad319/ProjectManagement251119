using ClosedXML.Excel;
using ProjectManagement.Client.Constant;
using ProjectManagement.Client.Helper;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using Xunit;

namespace ProjectManagement.Tests;

public class ExcelHelperTests
{
    [Fact]
    public void Import_LimitsTaskNameAndPreservesLongNameInNote()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");
        var longName = new string('A', FieldLengths.TaskName + 10);

        sheet.Cell(1, 1).Value = "1.01";
        sheet.Cell(1, 2).Value = longName;
        sheet.Cell(1, 4).Value = "m";
        sheet.Cell(1, 5).Value = 2;
        sheet.Cell(1, 6).Value = 10;

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        var task = Assert.Single(tasks);
        Assert.Equal(FieldLengths.TaskName, task.Name.Length);
        Assert.Equal(longName, task.Metadata.Note);
    }

    [Fact]
    public void Import_LimitsTaskCodeAndUnitToPersistedLengths()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = new string('C', FieldLengths.Code + 5);
        sheet.Cell(1, 2).Value = "Imported task";
        sheet.Cell(1, 4).Value = new string('m', FieldLengths.Unit + 5);
        sheet.Cell(1, 5).Value = 2;
        sheet.Cell(1, 6).Value = 10;

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        var task = Assert.Single(tasks);
        Assert.Equal(FieldLengths.Code, task.Metadata.Code.Length);
        Assert.Equal(FieldLengths.Unit, task.Unit.Length);
    }

    [Fact]
    public void Import_ThreeDashRow_ImportsThreeBarCodeTaskWithQuantityOneAndAllowsChildren()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = "YCQ.1112";
        sheet.Cell(1, 2).Value = "Kontrollplaner for vegetationsytor";
        sheet.Cell(1, 4).Value = "-";
        sheet.Cell(1, 5).Value = "-";
        sheet.Cell(1, 6).Value = "-";

        sheet.Cell(2, 1).Value = "YCQ.1112.1";
        sheet.Cell(2, 2).Value = "Child task";
        sheet.Cell(2, 4).Value = "m";
        sheet.Cell(2, 5).Value = 2;
        sheet.Cell(2, 6).Value = 10;

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        var task = Assert.Single(tasks);
        Assert.Equal(TaskType.ThreeBarCode, task.Metadata.Type);
        Assert.Equal(1, task.Quantity);
        Assert.Equal(1, task.Metadata.BaseQuantity);
        Assert.Equal(ConstValues.FixedQ, task.Metadata.QuantityParam);
        Assert.Equal("-", task.Unit);
        Assert.Single(task.Tasks);
    }

    [Fact]
    public void Import_FourDashRow_ImportsFourBarCodeLeafWithoutQuantityOrPrice()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = "YCQ.1112";
        sheet.Cell(1, 2).Value = "Kontrollplaner for vegetationsytor";
        sheet.Cell(1, 4).Value = "-";
        sheet.Cell(1, 5).Value = "-";
        sheet.Cell(1, 6).Value = "-";
        sheet.Cell(1, 8).Value = "-";

        sheet.Cell(2, 1).Value = "YCQ.1112.1";
        sheet.Cell(2, 2).Value = "Child task";
        sheet.Cell(2, 4).Value = "m";
        sheet.Cell(2, 5).Value = 2;
        sheet.Cell(2, 6).Value = 10;

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        Assert.Equal(2, tasks.Count);
        Assert.Equal(TaskType.FourBarCode, tasks[0].Metadata.Type);
        Assert.Null(tasks[0].Quantity);
        Assert.Equal("-", tasks[0].Unit);
        Assert.Null(tasks[0].Metadata.PriceSubDB);
        Assert.Empty(tasks[0].Tasks);
        Assert.Equal("YCQ.1112.1", tasks[1].Metadata.Code);
    }

    [Fact]
    public void Import_LocalNumberedRows_AreSiblingsUnderPreviousSectionCode()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = "BED.163";
        sheet.Cell(1, 2).Value = "Rivning av anlaggningskompletteringar till tunnel, bergrum o d";

        sheet.Cell(2, 1).Value = "1.";
        sheet.Cell(2, 2).Value = "Racke av rostfritt stal. Langd = 5,5 m, hojd 1,1 m.";
        sheet.Cell(2, 4).Value = "st";
        sheet.Cell(2, 5).Value = 1;
        sheet.Cell(2, 6).Value = 0;

        sheet.Cell(3, 1).Value = "2.";
        sheet.Cell(3, 2).Value = "Gangbrygga, inklusive racken, av rostfritt stal.";
        sheet.Cell(3, 4).Value = "st";
        sheet.Cell(3, 5).Value = 1;
        sheet.Cell(3, 6).Value = 0;

        sheet.Cell(4, 1).Value = "3.";
        sheet.Cell(4, 2).Value = "Stege av glasfiber.";
        sheet.Cell(4, 4).Value = "st";
        sheet.Cell(4, 5).Value = 1;
        sheet.Cell(4, 6).Value = 0;

        sheet.Cell(5, 1).Value = "4.";
        sheet.Cell(5, 2).Value = "Lejdare av rostfritt stal.";
        sheet.Cell(5, 4).Value = "st";
        sheet.Cell(5, 5).Value = 1;
        sheet.Cell(5, 6).Value = 0;

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        var section = Assert.Single(tasks);
        Assert.Equal("BED.163", section.Metadata.Code);
        Assert.Equal(4, section.Tasks.Count);
        Assert.Collection(
            section.Tasks,
            task =>
            {
                Assert.Empty(task.Metadata.Code);
                Assert.StartsWith("1. ", task.Name);
            },
            task =>
            {
                Assert.Empty(task.Metadata.Code);
                Assert.StartsWith("2. ", task.Name);
            },
            task =>
            {
                Assert.Empty(task.Metadata.Code);
                Assert.StartsWith("3. ", task.Name);
            },
            task =>
            {
                Assert.Empty(task.Metadata.Code);
                Assert.StartsWith("4. ", task.Name);
            });
        Assert.All(section.Tasks, task => Assert.Empty(task.Tasks));
    }

    [Fact]
    public void Import_PlainLocalNumberedRows_MovesCodeIntoName()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = "BED.163";
        sheet.Cell(1, 2).Value = "Rivning av anlaggningskompletteringar";

        sheet.Cell(2, 1).Value = "1";
        sheet.Cell(2, 2).Value = "Racke av rostfritt stal.";
        sheet.Cell(2, 4).Value = "st";
        sheet.Cell(2, 5).Value = 1;
        sheet.Cell(2, 6).Value = 0;

        sheet.Cell(3, 1).Value = "2";
        sheet.Cell(3, 2).Value = "Gangbrygga inklusive racken.";
        sheet.Cell(3, 4).Value = "st";
        sheet.Cell(3, 5).Value = 1;
        sheet.Cell(3, 6).Value = 0;

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        var section = Assert.Single(tasks);
        Assert.Equal("BED.163", section.Metadata.Code);
        Assert.Equal(2, section.Tasks.Count);
        Assert.Collection(
            section.Tasks,
            task =>
            {
                Assert.Empty(task.Metadata.Code);
                Assert.Equal("1 Racke av rostfritt stal.", task.Name);
            },
            task =>
            {
                Assert.Empty(task.Metadata.Code);
                Assert.Equal("2 Gangbrygga inklusive racken.", task.Name);
            });
    }

    [Fact]
    public void DetectLayout_UnlabeledSwedishRows_FindsColumnsAndStartRow()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 2).Value = "Project title";
        sheet.Cell(6, 1).Value = "BED.163";
        sheet.Cell(6, 2).Value = "Rivning av anlaggningskompletteringar till tunnel, bergrum o d";
        sheet.Cell(7, 1).Value = "1.";
        sheet.Cell(7, 2).Value = "Racke av rostfritt stal. Langd = 5,5 m, hojd 1,1 m.";
        sheet.Cell(7, 4).Value = "st";
        sheet.Cell(7, 5).Value = 1;
        sheet.Cell(7, 7).Value = 0;
        sheet.Cell(8, 1).Value = "2.";
        sheet.Cell(8, 2).Value = "Gangbrygga, inklusive racken, av rostfritt stal.";
        sheet.Cell(8, 4).Value = "st";
        sheet.Cell(8, 5).Value = 1;
        sheet.Cell(8, 7).Value = 0;

        var layout = ExcelHelper.DetectLayout(workbook);

        Assert.True(layout.IsDetected);
        Assert.Equal(6, layout.RowStart);
        Assert.Equal(1, layout.CodeIndex);
        Assert.Equal(2, layout.NameIndex);
        Assert.Equal(4, layout.UnitIndex);
        Assert.Equal(5, layout.QuantityIndex);
        Assert.Equal(7, layout.PriceIndex);
        Assert.Equal(8, layout.AmountIndex);
        Assert.Equal(8, layout.RowEnd);
    }

    [Fact]
    public void DetectLayout_HeaderRows_FindsStartEndAndAmountColumn()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Mängdförteckning");

        sheet.Cell(4, 1).Value = "Kod";
        sheet.Cell(4, 2).Value = "Namn";
        sheet.Cell(4, 3).Value = "Mängd";
        sheet.Cell(4, 4).Value = "Enhet";
        sheet.Cell(4, 5).Value = "Á-pris";
        sheet.Cell(4, 6).Value = "Belopp";

        sheet.Cell(5, 1).Value = "BED.163";
        sheet.Cell(5, 2).Value = "Rivning av anlaggningskompletteringar";
        sheet.Cell(6, 1).Value = "1";
        sheet.Cell(6, 2).Value = "Racke av rostfritt stal.";
        sheet.Cell(6, 3).Value = 1;
        sheet.Cell(6, 4).Value = "st";
        sheet.Cell(6, 5).Value = 0;
        sheet.Cell(6, 6).Value = 0;
        sheet.Cell(8, 1).Value = "Footer";

        var layout = ExcelHelper.DetectLayout(workbook);

        Assert.True(layout.IsDetected);
        Assert.Equal(5, layout.RowStart);
        Assert.Equal(6, layout.RowEnd);
        Assert.Equal(1, layout.CodeIndex);
        Assert.Equal(2, layout.NameIndex);
        Assert.Equal(4, layout.UnitIndex);
        Assert.Equal(3, layout.QuantityIndex);
        Assert.Equal(5, layout.PriceIndex);
        Assert.Equal(6, layout.AmountIndex);
    }
}

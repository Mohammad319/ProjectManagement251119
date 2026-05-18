using ClosedXML.Excel;
using ProjectManagement.Client.Helper;
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
}

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
    public void Import_PrefixCodes_BuildsNestedHierarchy()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = "B";
        sheet.Cell(1, 2).Value = "Main section";
        sheet.Cell(2, 1).Value = "BB";
        sheet.Cell(2, 2).Value = "Sub section";
        sheet.Cell(3, 1).Value = "BBB";
        sheet.Cell(3, 2).Value = "Deep section";
        sheet.Cell(4, 1).Value = "BBC.3";
        sheet.Cell(4, 2).Value = "Dotted section";
        sheet.Cell(5, 1).Value = "BBC.32";
        sheet.Cell(5, 2).Value = "Dotted child";

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        var root = Assert.Single(tasks);
        Assert.Equal("B", root.Metadata.Code);
        var bb = Assert.Single(root.Tasks);
        Assert.Equal("BB", bb.Metadata.Code);
        Assert.Equal(2, bb.Tasks.Count);
        Assert.Equal("BBB", bb.Tasks[0].Metadata.Code);
        Assert.Equal("BBC.3", bb.Tasks[1].Metadata.Code);
        var dottedChild = Assert.Single(bb.Tasks[1].Tasks);
        Assert.Equal("BBC.32", dottedChild.Metadata.Code);
    }

    [Fact]
    public void Import_AmaStyleCodes_BBCNestsUnderBBNotB()
    {
        // BBC starts with BB → must appear under BB, not directly under B.
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = "B";
        sheet.Cell(1, 2).Value = "Main section";
        sheet.Cell(2, 1).Value = "BB";
        sheet.Cell(2, 2).Value = "BB section";
        sheet.Cell(3, 1).Value = "BBB";
        sheet.Cell(3, 2).Value = "BBB section";
        sheet.Cell(4, 1).Value = "BBB.3";
        sheet.Cell(4, 2).Value = "BBB.3 section";
        sheet.Cell(5, 1).Value = "BBB.37";
        sheet.Cell(5, 2).Value = "BBB.37 section";
        sheet.Cell(6, 1).Value = "BBC";
        sheet.Cell(6, 2).Value = "BBC section";
        sheet.Cell(7, 1).Value = "BBC.3";
        sheet.Cell(7, 2).Value = "BBC.3 section";
        sheet.Cell(8, 1).Value = "BBC.32";
        sheet.Cell(8, 2).Value = "BBC.32 section";

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        var root = Assert.Single(tasks);
        Assert.Equal("B", root.Metadata.Code);
        var bb = Assert.Single(root.Tasks);
        Assert.Equal("BB", bb.Metadata.Code);
        Assert.Equal(2, bb.Tasks.Count);

        var bbb = bb.Tasks[0];
        Assert.Equal("BBB", bbb.Metadata.Code);
        var bbb3 = Assert.Single(bbb.Tasks);
        Assert.Equal("BBB.3", bbb3.Metadata.Code);
        var bbb37 = Assert.Single(bbb3.Tasks);
        Assert.Equal("BBB.37", bbb37.Metadata.Code);

        var bbc = bb.Tasks[1];
        Assert.Equal("BBC", bbc.Metadata.Code);
        var bbc3 = Assert.Single(bbc.Tasks);
        Assert.Equal("BBC.3", bbc3.Metadata.Code);
        var bbc32 = Assert.Single(bbc3.Tasks);
        Assert.Equal("BBC.32", bbc32.Metadata.Code);
    }

    [Fact]
    public void Import_AmaStyleCodes_BCIsSiblingOfBBUnderB()
    {
        // BC starts with B but not BB → BC must be a sibling of BB, not a child of BB.
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = "B";
        sheet.Cell(1, 2).Value = "Main section";
        sheet.Cell(2, 1).Value = "BB";
        sheet.Cell(2, 2).Value = "BB section";
        sheet.Cell(3, 1).Value = "BC";
        sheet.Cell(3, 2).Value = "BC section";
        sheet.Cell(4, 1).Value = "BE";
        sheet.Cell(4, 2).Value = "BE section";

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        var root = Assert.Single(tasks);
        Assert.Equal("B", root.Metadata.Code);
        Assert.Equal(3, root.Tasks.Count);
        Assert.Equal("BB", root.Tasks[0].Metadata.Code);
        Assert.Equal("BC", root.Tasks[1].Metadata.Code);
        Assert.Equal("BE", root.Tasks[2].Metadata.Code);
        Assert.Empty(root.Tasks[1].Tasks);
        Assert.Empty(root.Tasks[2].Tasks);
    }

    [Fact]
    public void Import_TextOnlyRows_AreCodeTextNotCalculationItems()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = "BBC";
        sheet.Cell(1, 2).Value = "Undersokningar";
        sheet.Cell(2, 2).Value = "Only descriptive text";

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        var root = Assert.Single(tasks);
        Assert.Equal(TaskType.CodeName, root.Metadata.Type);
        var child = Assert.Single(root.Tasks);
        Assert.Equal(TaskType.CodeName, child.Metadata.Type);
    }

    [Fact]
    public void Import_IncompleteCalculationRows_AreCalculationItemsForReview()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = "BBC.1";
        sheet.Cell(1, 2).Value = "Missing quantity";
        sheet.Cell(1, 4).Value = "m";

        sheet.Cell(2, 1).Value = "BBC.2";
        sheet.Cell(2, 2).Value = "Dash in unit";
        sheet.Cell(2, 4).Value = "-";

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        Assert.Equal(2, tasks.Count);
        Assert.All(tasks, task => Assert.Equal(TaskType.Task, task.Metadata.Type));
        Assert.Null(tasks[0].Quantity);
        Assert.Equal("m", tasks[0].Unit);
        Assert.Null(tasks[1].Quantity);
        Assert.Equal("-", tasks[1].Unit);
    }

    [Fact]
    public void Import_DashPatterns_ClassifiesThreeAndFourBarCodesWithoutPrice()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = "A";
        sheet.Cell(1, 2).Value = "Three dash";
        sheet.Cell(1, 4).Value = "-";
        sheet.Cell(1, 5).Value = "-";
        sheet.Cell(1, 6).Value = "-";

        sheet.Cell(2, 1).Value = "B";
        sheet.Cell(2, 2).Value = "Four dash";
        sheet.Cell(2, 4).Value = "-";
        sheet.Cell(2, 5).Value = "-";
        sheet.Cell(2, 6).Value = "-";
        sheet.Cell(2, 7).Value = "-";

        var tasks = ExcelHelper.Import(1, workbook, 1, 2, 1, 2, 4, 5, 6, 7, isOH: false);

        Assert.Equal(2, tasks.Count);
        Assert.Equal(TaskType.ThreeBarCode, tasks[0].Metadata.Type);
        Assert.Equal(TaskType.FourBarCode, tasks[1].Metadata.Type);
        Assert.Null(tasks[0].Metadata.PriceSubDB);
        Assert.Null(tasks[1].Metadata.PriceSubDB);
    }

    [Fact]
    public void Import_PriceZeroAndErrorValues_AreIgnored()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = "BBC.1";
        sheet.Cell(1, 2).Value = "Valid task with zero price";
        sheet.Cell(1, 4).Value = "m";
        sheet.Cell(1, 5).Value = 2;
        sheet.Cell(1, 6).Value = 0;

        sheet.Cell(2, 1).Value = "BBC.2";
        sheet.Cell(2, 2).Value = "Valid task with price error";
        sheet.Cell(2, 4).Value = "m";
        sheet.Cell(2, 5).Value = 2;
        sheet.Cell(2, 6).Value = "#VÄRDEFEL!";

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        Assert.Equal(2, tasks.Count);
        Assert.All(tasks, task =>
        {
            Assert.Equal(TaskType.Task, task.Metadata.Type);
            Assert.Null(task.Metadata.PriceSubDB);
        });
    }

    [Fact]
    public void Import_BsabColonCodes_BuildsCorrectHierarchy()
    {
        // BV:EBB/B must nest under BV:EBB, not BV:EB or BV:E.
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = "BV";       sheet.Cell(1, 2).Value = "Byggnadsverk";
        sheet.Cell(2, 1).Value = "BV:E";     sheet.Cell(2, 2).Value = "E-grupp";
        sheet.Cell(3, 1).Value = "BV:EB";    sheet.Cell(3, 2).Value = "EB-grupp";
        sheet.Cell(4, 1).Value = "BV:EBA";   sheet.Cell(4, 2).Value = "EBA";
        sheet.Cell(5, 1).Value = "BV:EBB";   sheet.Cell(5, 2).Value = "EBB";
        sheet.Cell(6, 1).Value = "BV:EBB/B"; sheet.Cell(6, 2).Value = "EBB/B";
        sheet.Cell(7, 1).Value = "BV:EBB/C"; sheet.Cell(7, 2).Value = "EBB/C";

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        var root = Assert.Single(tasks);
        Assert.Equal("BV", root.Metadata.Code);
        var bvE = Assert.Single(root.Tasks);
        Assert.Equal("BV:E", bvE.Metadata.Code);
        var bvEb = Assert.Single(bvE.Tasks);
        Assert.Equal("BV:EB", bvEb.Metadata.Code);
        Assert.Equal(2, bvEb.Tasks.Count);
        Assert.Equal("BV:EBA", bvEb.Tasks[0].Metadata.Code);
        var bvEbb = bvEb.Tasks[1];
        Assert.Equal("BV:EBB", bvEbb.Metadata.Code);
        Assert.Equal(2, bvEbb.Tasks.Count);
        Assert.Equal("BV:EBB/B", bvEbb.Tasks[0].Metadata.Code);
        Assert.Equal("BV:EBB/C", bvEbb.Tasks[1].Metadata.Code);
    }

    [Fact]
    public void Import_BsabNumericLetterCodes_BuildsCorrectHierarchy()
    {
        // 31.BC must nest under 31.B, and 31.B under 31, and 31 under 3.
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = "3";     sheet.Cell(1, 2).Value = "Byggdelar";
        sheet.Cell(2, 1).Value = "31";    sheet.Cell(2, 2).Value = "Grundläggning";
        sheet.Cell(3, 1).Value = "31.B";  sheet.Cell(3, 2).Value = "Pålning";
        sheet.Cell(4, 1).Value = "31.BC"; sheet.Cell(4, 2).Value = "Stålpålar";
        sheet.Cell(5, 1).Value = "31.BD"; sheet.Cell(5, 2).Value = "Träpålar";
        sheet.Cell(6, 1).Value = "31.C";  sheet.Cell(6, 2).Value = "Spontning";
        sheet.Cell(7, 1).Value = "31.CB"; sheet.Cell(7, 2).Value = "Spont stål";
        sheet.Cell(8, 1).Value = "31.E";  sheet.Cell(8, 2).Value = "Jordankare";
        sheet.Cell(9, 1).Value = "31.EB"; sheet.Cell(9, 2).Value = "Injektionsankare";

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        var root = Assert.Single(tasks);
        Assert.Equal("3", root.Metadata.Code);
        var t31 = Assert.Single(root.Tasks);
        Assert.Equal("31", t31.Metadata.Code);
        Assert.Equal(3, t31.Tasks.Count);

        var t31b = t31.Tasks[0];
        Assert.Equal("31.B", t31b.Metadata.Code);
        Assert.Equal(2, t31b.Tasks.Count);
        Assert.Equal("31.BC", t31b.Tasks[0].Metadata.Code);
        Assert.Equal("31.BD", t31b.Tasks[1].Metadata.Code);

        var t31c = t31.Tasks[1];
        Assert.Equal("31.C", t31c.Metadata.Code);
        var t31cb = Assert.Single(t31c.Tasks);
        Assert.Equal("31.CB", t31cb.Metadata.Code);

        var t31e = t31.Tasks[2];
        Assert.Equal("31.E", t31e.Metadata.Code);
        var t31eb = Assert.Single(t31e.Tasks);
        Assert.Equal("31.EB", t31eb.Metadata.Code);
    }

    [Fact]
    public void Import_NumericDotCodes_OnePointOneAndOnePointTwelveAreSiblings()
    {
        // 1.12 must NOT nest under 1.1 — they are siblings under 1.
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = "1";    sheet.Cell(1, 2).Value = "Kapitel 1";
        sheet.Cell(2, 1).Value = "1.1";  sheet.Cell(2, 2).Value = "Avsnitt 1.1";
        sheet.Cell(3, 1).Value = "1.12"; sheet.Cell(3, 2).Value = "Avsnitt 1.12";
        sheet.Cell(4, 1).Value = "2";    sheet.Cell(4, 2).Value = "Kapitel 2";
        sheet.Cell(5, 1).Value = "2.1";  sheet.Cell(5, 2).Value = "Avsnitt 2.1";
        sheet.Cell(6, 1).Value = "2.2";  sheet.Cell(6, 2).Value = "Avsnitt 2.2";

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        Assert.Equal(2, tasks.Count);

        var ch1 = tasks[0];
        Assert.Equal("1", ch1.Metadata.Code);
        Assert.Equal(2, ch1.Tasks.Count);
        Assert.Equal("1.1", ch1.Tasks[0].Metadata.Code);
        Assert.Equal("1.12", ch1.Tasks[1].Metadata.Code);
        Assert.Empty(ch1.Tasks[0].Tasks);
        Assert.Empty(ch1.Tasks[1].Tasks);

        var ch2 = tasks[1];
        Assert.Equal("2", ch2.Metadata.Code);
        Assert.Equal(2, ch2.Tasks.Count);
        Assert.Equal("2.1", ch2.Tasks[0].Metadata.Code);
        Assert.Equal("2.2", ch2.Tasks[1].Metadata.Code);
    }

    [Fact]
    public void Import_NumericSlashCodes_ExactPrefixDeterminesParent()
    {
        // 21/800.81 must nest under 21/800 (exact prefix).
        // 21/300, 21/800, 21/900 are siblings under 21.
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Import");

        sheet.Cell(1, 1).Value = "21";        sheet.Cell(1, 2).Value = "Kap 21";
        sheet.Cell(2, 1).Value = "21/300";    sheet.Cell(2, 2).Value = "Grupp 300";
        sheet.Cell(3, 1).Value = "21/800";    sheet.Cell(3, 2).Value = "Grupp 800";
        sheet.Cell(4, 1).Value = "21/800.81"; sheet.Cell(4, 2).Value = "Post 800.81";
        sheet.Cell(5, 1).Value = "21/900";    sheet.Cell(5, 2).Value = "Grupp 900";

        var tasks = ExcelHelper.Import(1, workbook, 1, 1, 2, 4, 5, 6, isOH: false);

        var root = Assert.Single(tasks);
        Assert.Equal("21", root.Metadata.Code);
        Assert.Equal(3, root.Tasks.Count);
        Assert.Equal("21/300", root.Tasks[0].Metadata.Code);
        var g800 = root.Tasks[1];
        Assert.Equal("21/800", g800.Metadata.Code);
        var leaf = Assert.Single(g800.Tasks);
        Assert.Equal("21/800.81", leaf.Metadata.Code);
        Assert.Equal("21/900", root.Tasks[2].Metadata.Code);
        Assert.Empty(root.Tasks[2].Tasks);
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
        Assert.Equal(8, layout.RowEnd);
        Assert.Equal(1, layout.CodeIndex);
        Assert.Equal(2, layout.NameIndex);
        Assert.Equal(4, layout.UnitIndex);
        Assert.Equal(3, layout.QuantityIndex);
        Assert.Equal(5, layout.PriceIndex);
        Assert.Equal(6, layout.AmountIndex);
    }

    [Fact]
    public void DetectLayout_HeaderRows_StartsAtNameTextEvenWhenCodeIsEmpty()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Mängdförteckning");

        sheet.Cell(4, 1).Value = "Kod";
        sheet.Cell(4, 2).Value = "Namn";
        sheet.Cell(4, 3).Value = "Mängd";
        sheet.Cell(4, 4).Value = "Enhet";
        sheet.Cell(5, 2).Value = "Allmän orientering";
        sheet.Cell(6, 1).Value = "B";
        sheet.Cell(6, 2).Value = "Huvudkod";

        var layout = ExcelHelper.DetectLayout(workbook);

        Assert.True(layout.IsDetected);
        Assert.Equal(5, layout.RowStart);
    }

    [Fact]
    public void DetectLayout_EndRow_AllowsEmptyRowsInsideImport()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Mängdförteckning");

        sheet.Cell(1, 1).Value = "Kod";
        sheet.Cell(1, 2).Value = "Namn";
        sheet.Cell(1, 3).Value = "Mängd";
        sheet.Cell(1, 4).Value = "Enhet";
        sheet.Cell(2, 1).Value = "B";
        sheet.Cell(2, 2).Value = "Start";
        sheet.Cell(12, 1).Value = "BB";
        sheet.Cell(12, 2).Value = "After empty rows";

        var layout = ExcelHelper.DetectLayout(workbook);

        Assert.True(layout.IsDetected);
        Assert.Equal(2, layout.RowStart);
        Assert.Equal(12, layout.RowEnd);
    }

    [Fact]
    public void DetectLayout_HeaderRows_UsesStrongHeadersForUnitQuantityPriceAndAmount()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Mängdförteckning");

        sheet.Cell(3, 1).Value = "Kod";
        sheet.Cell(3, 2).Value = "Text";
        sheet.Cell(3, 3).Value = "Enhet";
        sheet.Cell(3, 4).Value = "Mängd";
        sheet.Cell(3, 5).Value = "Á-pris";
        sheet.Cell(3, 6).Value = "Belopp";
        sheet.Cell(4, 1).Value = "NBK.1139";
        sheet.Cell(4, 2).Value = "Trappor av rostfritt stål";
        sheet.Cell(5, 2).Value = "A Här pratar vi om A";
        sheet.Cell(5, 3).Value = "-";
        sheet.Cell(5, 4).Value = "-";
        sheet.Cell(5, 5).Value = "-";
        sheet.Cell(5, 6).Value = "-";

        var layout = ExcelHelper.DetectLayout(workbook);

        Assert.True(layout.IsDetected);
        Assert.Equal(3, layout.UnitIndex);
        Assert.Equal(4, layout.QuantityIndex);
        Assert.Equal(5, layout.PriceIndex);
        Assert.Equal(6, layout.AmountIndex);
    }
}

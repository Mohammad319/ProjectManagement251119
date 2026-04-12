using System.Globalization;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Pages.Project.Storage.App;
using ProjectManagement.Client.Pages.Calculation.Table.SectionsList;
using ProjectManagement.Client.Pages.Calculation.Table.SectionsList.Rows;
using ProjectManagement.Client.Shared.Components;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using ProjectManagement.Shared.Helper;
using Xunit;

namespace ProjectManagement.Tests;

public class NumericFormattingTests : BunitContext
{
    [Theory]
    [InlineData("15", "15")]
    [InlineData("15.5", "15.5")]
    [InlineData("15.44", "15.44")]
    [InlineData("15.657", "15.657")]
    [InlineData("15.6578", "15.6578")]
    public void NumericFormatHelper_OmitsTrailingZeros(string rawValue, string expected)
    {
        var value = decimal.Parse(rawValue, CultureInfo.InvariantCulture);
        var actual = NumericFormatHelper.Format(value, 4, CultureInfo.InvariantCulture);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NumericFormatHelper_TruncatesToConfiguredDigits()
    {
        var value = decimal.Parse("15.657", CultureInfo.InvariantCulture);

        var actual = NumericFormatHelper.Format(value, 2, CultureInfo.InvariantCulture);

        Assert.Equal("15.65", actual);
    }

    [Theory]
    [InlineData("5", "5")]
    [InlineData("5.5", "5.5")]
    [InlineData("5.55", "5.55")]
    [InlineData("1.000000", "1")]
    [InlineData("120.000000", "120")]
    [InlineData("0.123456789", "0.123456789")]
    public void NumericDisplayHelper_KeepsExistingFractionDigitsWithoutTrailingZeros(string rawValue, string expected)
    {
        var value = decimal.Parse(rawValue, CultureInfo.InvariantCulture);

        var actual = NumericDisplayHelper.Format(value, CultureInfo.InvariantCulture);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NumericDisplayHelper_UsesCurrentCultureDecimalSeparator()
    {
        using var _ = new CultureScope("sv-SE");

        var actual = NumericDisplayHelper.Format(5.55m);

        Assert.Equal("5,55", actual);
    }

    [Fact]
    public void GroupTreeHelpersWasm_RangeLabel_TruncatesToTwoDigitsWithoutTrailingZeros()
    {
        var label = GroupTreeHelpersWasm.RangeLabel(15.657m, 20.1000m);
        var expected = $"[{NumericFormatHelper.Format(15.657m, 2)} .. {NumericFormatHelper.Format(20.1000m, 2)}]";

        Assert.Equal(expected, label);
    }

    [Fact]
    public void GroupTreeHelpersWasm_RangeLabel_KeepsWholeNumbersCompact()
    {
        var label = GroupTreeHelpersWasm.RangeLabel(15m, 20m);
        var expected = $"[{NumericFormatHelper.Format(15m, 2)} .. {NumericFormatHelper.Format(20m, 2)}]";

        Assert.Equal(expected, label);
    }

    [Fact]
    public void TrimmedNumberInput_RendersWithoutStoredScale()
    {
        var value = decimal.Parse("1.000000", CultureInfo.InvariantCulture);

        var cut = Render<TrimmedNumberInput<decimal>>(parameters => parameters
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<decimal>(this, _ => { }))
            .Add(x => x.ValueExpression, () => value)
            .Add(x => x.MaxFractionDigits, 4));

        Assert.Equal("1", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void TrimmedNumberInput_TruncatesDisplayedValue()
    {
        var value = decimal.Parse("15.657", CultureInfo.InvariantCulture);

        var cut = Render<TrimmedNumberInput<decimal>>(parameters => parameters
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<decimal>(this, _ => { }))
            .Add(x => x.ValueExpression, () => value)
            .Add(x => x.MaxFractionDigits, 2));

        Assert.Equal(NumericFormatHelper.Format(value, 2), cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void TrimmedNumberInput_AcceptsDotAndDisplaysSwedishComma()
    {
        using var _ = new CultureScope("sv-SE");

        decimal currentValue = 0m;
        Expression<Func<decimal>> valueExpression = () => currentValue;

        var cut = Render<TrimmedNumberInput<decimal>>(parameters => parameters
            .Add(x => x.Value, currentValue)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<decimal>(this, value => currentValue = value))
            .Add(x => x.ValueExpression, valueExpression)
            .Add(x => x.MaxFractionDigits, 2));

        cut.Find("input").Change("5.55");

        Assert.Equal(5.55m, currentValue);
        Assert.Equal("5,55", cut.Find("input").GetAttribute("value"));
    }

    [Theory]
    [InlineData("5.55", 5.55)]
    [InlineData("5,55", 5.55)]
    [InlineData("1 234,5", 1234.5)]
    public void NumericInputHelper_AcceptsEnglishAndSwedishSeparators(string rawValue, decimal expected)
    {
        using var _ = new CultureScope("sv-SE");

        var success = NumericInputHelper.TryParseDecimal(rawValue, out var actual);

        Assert.True(success);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TableRenderHelpers_TruncateFormattedTableValue()
    {
        using var _ = new CultureScope("en-US");

        var cut = Render(builder =>
        {
            builder.OpenElement(0, "table");
            builder.OpenElement(1, "tbody");
            builder.OpenElement(2, "tr");
            TableRenderHelpers.RenderFormattedTd(builder, "0.##", decimal.Parse("15.657", CultureInfo.InvariantCulture));
            builder.CloseElement();
            builder.CloseElement();
            builder.CloseElement();
        });

        Assert.Contains("15.65", cut.Markup);
        Assert.DoesNotContain("15.66", cut.Markup);
    }

    [Fact]
    public void TableRenderHelpers_UsesCurrentCultureSeparator()
    {
        using var _ = new CultureScope("sv-SE");

        var cut = Render(builder =>
        {
            builder.OpenElement(0, "table");
            builder.OpenElement(1, "tbody");
            builder.OpenElement(2, "tr");
            TableRenderHelpers.RenderFormattedTd(builder, "0.##", decimal.Parse("15.657", CultureInfo.InvariantCulture));
            builder.CloseElement();
            builder.CloseElement();
            builder.CloseElement();
        });

        Assert.Contains("15,65", cut.Markup);
    }

    [Fact]
    public void ResourceDetailRows_UsesConfiguredFractionDigits()
    {
        ComponentFactories.AddStub<NotesRows>();

        var resource = new ResourceListMVVM
        {
            Data = new ResourceMetadata
            {
                Parameters =
                [
                    new ResourceParameter
                    {
                        Name = "Width",
                        Unit = "m",
                        Value = decimal.Parse("15.6570", CultureInfo.InvariantCulture)
                    }
                ],
                Times =
                [
                    new ResourceTime
                    {
                        Name = "Labor",
                        Cost = decimal.Parse("15.4400", CultureInfo.InvariantCulture)
                    }.SetResolvedQuantity(decimal.Parse("15.5000", CultureInfo.InvariantCulture))
                ]
            }
        };

        var cut = Render<ResourceDetailRows>(parameters => parameters
            .Add(x => x.Resource, resource)
            .Add(x => x.Colmuns, CreateDetailColumns())
            .Add(x => x.MaxFractionDigits, 2)
            .Add(x => x.Color, "#88aadd"));

        var markup = cut.Markup;
        var expectedParameter = NumericFormatHelper.Format(resource.Data.Parameters[0].Value, 2);
        var expectedTimeQuantity = NumericFormatHelper.Format(resource.Data.Times[0].Quantity, 2);
        var expectedTimeCost = NumericFormatHelper.Format(resource.Data.Times[0].Cost, 2);

        Assert.True(markup.Contains(expectedParameter), markup);
        Assert.True(markup.Contains(expectedTimeQuantity), markup);
        Assert.True(markup.Contains(expectedTimeCost), markup);
        Assert.DoesNotContain("15.657", markup);
        Assert.DoesNotContain("15.5000", markup);
        Assert.DoesNotContain("15.4400", markup);
    }

    private static IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> CreateDetailColumns() =>
    [
        new() { Id = NetColumnId.Name },
        new() { Id = NetColumnId.Quantity },
        new() { Id = NetColumnId.Unit },
        new() { Id = NetColumnId.Cost }
    ];

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUiCulture;
        }
    }
}

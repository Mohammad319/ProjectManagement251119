using Domain.Entities.Calculation;
using ProjectManagement.Shared.Constants;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System.Text.Json;
using Xunit;

namespace ProjectManagement.Tests;

public class TemplateMetadataTests
{
    [Fact]
    public void TemplateEntity_MetadataSnapshot_PreservesStyleAndReadsColumnsFromColumnEntity()
    {
        var metadata = new TemplateData
        {
            MathRound = 4,
            Currency = "SEK",
            DateFormat = "yyyy-MM-dd",
            NetCalc = new NetCalc
            {
                Color = new NetColor
                {
                    ResourceParameter = "#123456",
                    ResourceAttachment = "#234567",
                    ResourceTime = "#345678",
                    TaskCodeName = "#456789",
                    TaskDetailBaseQuantity = "#56789a"
                },
                Columns =
                [
                    new() { Id = NetColumnId.Name, Width = 120, Frozen = true },
                    new() { Id = NetColumnId.Quantity, Width = 70, Frozen = false }
                ],
                Sort = new SortConfig
                {
                    TaskColumn = NetColumnId.Quantity,
                    TaskDescending = true,
                    ResourceColumn = NetColumnId.Name,
                    ResourceDescending = true
                }
            }
        };

        var entity = new TemplateEntity("Default", true, 1);
        entity.UpdateMetadata(metadata);

        var columnSettings = new TemplateColumnEntity("Default", true, 1, metadata.NetCalc.Columns);
        var snapshot = entity.GetMetadataSnapshot(columnSettings.GetColumnsSnapshot());

        Assert.Equal(4, snapshot.MathRound);
        Assert.Equal("#123456", snapshot.NetCalc.Color.ResourceParameter);
        Assert.Equal("#234567", snapshot.NetCalc.Color.ResourceAttachment);
        Assert.Equal("#345678", snapshot.NetCalc.Color.ResourceTime);
        Assert.Equal("#456789", snapshot.NetCalc.Color.TaskCodeName);
        Assert.Equal("#56789a", snapshot.NetCalc.Color.TaskDetailBaseQuantity);
        Assert.Null(snapshot.NetCalc.Sort.TaskColumn);
        Assert.False(snapshot.NetCalc.Sort.TaskDescending);
        Assert.Null(snapshot.NetCalc.Sort.ResourceColumn);
        Assert.False(snapshot.NetCalc.Sort.ResourceDescending);
        Assert.DoesNotContain(snapshot.NetCalc.Columns, x => x.Id == NetColumnId.ChangeFactor1);
        Assert.DoesNotContain(snapshot.NetCalc.Columns, x => x.Id == NetColumnId.PriceProduction);
    }

    [Fact]
    public void CalculationEntity_Update_PreservesSortConfigOnCalculation()
    {
        var dto = new CalculationPostDTO
        {
            Code = "C-1",
            Name = "Calculation",
            Sort = new SortConfig
            {
                TaskColumn = NetColumnId.Quantity,
                TaskDescending = true,
                ResourceColumn = NetColumnId.Name,
                ResourceDescending = true
            }
        };

        var entity = new CalculationEntity();
        entity.Update(dto);

        Assert.Equal(NetColumnId.Quantity, entity.Sort.TaskColumn);
        Assert.True(entity.Sort.TaskDescending);
        Assert.Equal(NetColumnId.Name, entity.Sort.ResourceColumn);
        Assert.True(entity.Sort.ResourceDescending);
    }

    [Fact]
    public void TemplateListPostDto_JsonRoundTrip_PreservesMathRound()
    {
        var dto = new TemplateListPostDTO
        {
            Name = "Default",
            MathRound = 5
        };

        var json = JsonSerializer.Serialize(dto);
        var restored = JsonSerializer.Deserialize<TemplateListPostDTO>(json);

        Assert.Contains(nameof(TemplateListPostDTO.MathRound), json);
        Assert.NotNull(restored);
        Assert.Equal(5, restored!.MathRound);
    }
}

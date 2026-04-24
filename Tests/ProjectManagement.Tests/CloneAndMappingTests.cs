using ProjectManagement.Client.Shared.Mapping;
using ProjectManagement.Client.Shared.Model.Application;
using ProjectManagement.Shared.Base.Application;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Offer;
using System.Text.Json;
using Xunit;

namespace ProjectManagement.Tests;

public class CloneAndMappingTests
{
    [Fact]
    public void TaskPostDto_MetadataSetter_ClonesNestedMetadata()
    {
        var source = new TaskMetadata
        {
            Note = "task-note",
            Unit = "m",
            Code = "T-01",
            Quantity = 2.5m,
            PriceProductionDB = 12.34m,
            UpperNote = ["line-1"]
        };

        var dto = new TaskPostDTO
        {
            Metadata = source
        };

        source.Note = "changed";
        source.Code = "changed";
        source.UpperNote[0] = "changed-line";
        source.UpperNote.Add("line-2");

        Assert.NotSame(source, dto.Metadata);
        Assert.Equal("task-note", dto.Metadata.Note);
        Assert.Equal("T-01", dto.Metadata.Code);
        Assert.Equal(12.34m, dto.Metadata.PriceProductionDB);
        Assert.Single(dto.Metadata.UpperNote);
        Assert.Equal("line-1", dto.Metadata.UpperNote[0]);
    }

    [Fact]
    public void ResourcePostDto_DataSetter_ClonesCollectionsAndNestedObjects()
    {
        var source = new ResourceMetadata
        {
            Note = "resource-note",
            Unit = "kg",
            Cost = 15m,
            UpperNote = ["upper-1"],
            Parameters =
            [
                new ResourceParameter { Name = "length", Unit = "m", Value = 4m }
            ],
            Times =
            [
                new ResourceTime { Name = "install", Percentage = 50m, Cost = 7m }.SetResolvedQuantity(2m)
            ]
        };

        var dto = new ResourcePostDTO
        {
            Data = source
        };

        source.Note = "changed";
        source.UpperNote[0] = "changed-upper";
        source.Parameters[0].Name = "changed-parameter";
        source.Times[0].Cost = 99m;

        Assert.NotSame(source, dto.Data);
        Assert.NotSame(source.Parameters, dto.Data.Parameters);
        Assert.NotSame(source.Times, dto.Data.Times);
        Assert.Equal("resource-note", dto.Data.Note);
        Assert.Equal("upper-1", dto.Data.UpperNote[0]);
        Assert.Equal("length", dto.Data.Parameters[0].Name);
        Assert.Equal(50m, dto.Data.Times[0].Percentage);
        Assert.Equal(7m, dto.Data.Times[0].Cost);
    }

    [Fact]
    public void ResourceTime_JsonRoundTrip_PreservesQuantityWithPrivateSetter()
    {
        var source = new ResourceTime
        {
            Name = "install",
            Percentage = 50m,
            Cost = 7m
        }.SetResolvedQuantity(2m);

        var json = JsonSerializer.Serialize(source);
        var restored = JsonSerializer.Deserialize<ResourceTime>(json);

        Assert.NotNull(restored);
        Assert.Equal(2m, restored!.Quantity);
        Assert.Equal(50m, restored.Percentage);
        Assert.Equal(7m, restored.Cost);
    }

    [Fact]
    public void PostOfferDto_DataSetter_ClonesStatusAndContact()
    {
        var source = new OfferData
        {
            Contact = "Anna Andersson",
            Status = "Förbereds",
            Cost = 15m,
            BaseCost = 2m
        };

        var dto = new PostOfferDTO
        {
            Data = source
        };

        source.Contact = "Changed";
        source.Status = "Changed";

        Assert.NotSame(source, dto.Data);
        Assert.Equal("Anna Andersson", dto.Contact);
        Assert.Equal("Förbereds", dto.Status);
        Assert.Equal(15m, dto.Cost);
        Assert.Equal(2m, dto.BaseCost);
    }

    [Fact]
    public void ClientDtoMapper_ToCalculationMVVM_ClearsActiveDisplayPresetOnLoad()
    {
        var preset = new DisplayOptionsPreset
        {
            Id = "preset-1",
            Name = "Saved preset"
        };

        var dto = new CalculationPageDTO
        {
            DisplayPresets = new DisplayOptionsPresetStore
            {
                ActivePresetId = preset.Id,
                Presets = [preset]
            }
        };

        var model = dto.ToCalculationMVVM();

        Assert.Null(model.DisplayPresets.ActivePresetId);
        Assert.Single(model.DisplayPresets.Presets);
        Assert.Null(DisplayOptionsPresetState.GetActivePreset(model.DisplayPresets));
    }

    [Fact]
    public void ApplicationDto_DataSetter_ClonesRowsAndAttributes()
    {
        var rowId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();

        var source = new ApplicationDataDTO
        {
            Description = "application-desc",
            Rows =
            [
                new RowDTO
                {
                    ID = rowId,
                    Name = "row-1",
                    Attributes =
                    [
                        new AttributeDTO
                        {
                            ID = attributeId,
                            AttributeType = AttributeType.Text,
                            Value = "value-1",
                            Validation = "{\"min\":1}",
                            Style = "font-weight:bold"
                        }
                    ]
                }
            ]
        };

        var dto = new ApplicationDTO
        {
            Data = source
        };

        source.Description = "changed";
        source.Rows[0].Name = "changed-row";
        source.Rows[0].Attributes[0].Value = "changed-value";

        Assert.NotSame(source, dto.Data);
        Assert.NotSame(source.Rows, dto.Data.Rows);
        Assert.Equal("application-desc", dto.Data.Description);
        Assert.Equal("row-1", dto.Data.Rows[0].Name);
        Assert.Equal("value-1", dto.Data.Rows[0].Attributes[0].Value);
    }

    [Fact]
    public void ClientDtoMapper_ToApplicationValuesModel_DeepCopiesSourceData()
    {
        var key = Guid.NewGuid();
        var dto = new ApplicationValuesDTO
        {
            Id = 12,
            CalculationId = 25,
            ApplicationId = 7,
            Name = "calc-app",
            Responsible = "owner",
            Data = new ApplicationValuesData
            {
                Attributes = new Dictionary<Guid, string>
                {
                    [key] = "alpha"
                }
            },
            Application = new ApplicationDTO
            {
                Id = 7,
                Name = "template",
                Data = new ApplicationDataDTO
                {
                    Description = "template-desc",
                    Rows =
                    [
                        new RowDTO
                        {
                            ID = Guid.NewGuid(),
                            Name = "row-a",
                            Attributes =
                            [
                                new AttributeDTO
                                {
                                    ID = Guid.NewGuid(),
                                    AttributeType = AttributeType.Text,
                                    Value = "v1"
                                }
                            ]
                        }
                    ]
                }
            }
        };

        var model = dto.ToApplicationValuesModel();

        dto.Data.Attributes[key] = "changed";
        dto.Application!.Data.Rows[0].Attributes[0].Value = "changed-value";
        dto.Application.Data.Rows.Add(new RowDTO { ID = Guid.NewGuid(), Name = "row-b" });

        Assert.Equal("alpha", model.Data.Attributes[key]);
        Assert.Equal("v1", model.Application.Data.Row[0].Attributes[0].Value);
        Assert.Single(model.Application.Data.Row);
    }

    [Fact]
    public void ClientDtoMapper_ToApplicationValuesDto_DeepCopiesModelData()
    {
        var key = Guid.NewGuid();
        var model = new ApplicationValuesModel
        {
            Id = 30,
            CalculationId = 50,
            ApplicationId = 9,
            Name = "form-values",
            Responsible = "planner",
            Data = new ApplicationValuesData
            {
                Attributes = new Dictionary<Guid, string>
                {
                    [key] = "beta"
                }
            },
            Application = new ApplicationModel
            {
                Id = 9,
                Name = "dynamic-form",
                Data = new ApplicationDataModel
                {
                    Description = "dynamic-desc",
                    Row =
                    [
                        new RowModel
                        {
                            ID = Guid.NewGuid(),
                            Name = "row-x",
                            Attributes =
                            [
                                new AttributeModel
                                {
                                    ID = Guid.NewGuid(),
                                    AttributeType = AttributeType.Text,
                                    Value = "original"
                                }
                            ]
                        }
                    ]
                }
            }
        };

        var dto = model.ToApplicationValuesDto();

        model.Data.Attributes[key] = "changed";
        model.Application.Data.Row[0].Attributes[0].Value = "changed-value";
        model.Application.Data.Row.Add(new RowModel { ID = Guid.NewGuid(), Name = "row-y" });

        Assert.Equal("beta", dto.Data.Attributes[key]);
        Assert.Equal("original", dto.Application!.Data.Rows[0].Attributes[0].Value);
        Assert.Single(dto.Application.Data.Rows);
    }
}

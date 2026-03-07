using Application.Feature.Application.Commands;
using Application.Feature.Application.Queries;
using Domain.Entities.Application;
using ProjectManagement.Client.Shared.Model.Application;
using ProjectManagement.Shared.Base.Application;

namespace ProjectManagement.Services.UI;

public interface IApplicationValuesViewService
{
    Task<List<ApplicationModel>> GetApplicationsAsync(bool withNoneVisible, CancellationToken ct = default);
    Task<int> CreateAsync(ApplicationValuesModel model, CancellationToken ct = default);
    Task<bool> UpdateAsync(ApplicationValuesModel model, CancellationToken ct = default);
}

public sealed class ApplicationValuesViewService(ICommandDispatcher dispatcher) : IApplicationValuesViewService
{
    public async Task<List<ApplicationModel>> GetApplicationsAsync(bool withNoneVisible, CancellationToken ct = default)
    {
        var items = await dispatcher.Send(new GetApplicationQuery(withNoneVisible), ct);
        return items?.Select(MapApplication).ToList() ?? [];
    }

    public Task<int> CreateAsync(ApplicationValuesModel model, CancellationToken ct = default)
        => dispatcher.Send(new CreateCalcAppCommand(ToBaseModel(model), model.CalculationId, model.ApplicationId), ct);

    public Task<bool> UpdateAsync(ApplicationValuesModel model, CancellationToken ct = default)
        => dispatcher.Send(new UpdateCalcAppCommand(ToEntity(model)), ct);

    private static ApplicationValuesBase ToBaseModel(ApplicationValuesModel model)
        => new()
        {
            UserId = model.UserId,
            Name = model.Name,
            Responsible = model.Responsible,
            LastUpdate = model.LastUpdate,
            Data = model.Data ?? new ApplicationValuesData()
        };

    private static ApplicationValuesEntity ToEntity(ApplicationValuesModel model)
        => new()
        {
            Id = model.Id,
            CalculationId = model.CalculationId,
            ApplicationId = model.ApplicationId,
            UserId = model.UserId,
            Name = model.Name,
            Responsible = model.Responsible,
            LastUpdate = model.LastUpdate,
            Data = model.Data ?? new ApplicationValuesData()
        };

    private static ApplicationModel MapApplication(ApplicationEntity entity)
        => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            IsVisible = entity.IsVisible,
            UserId = entity.UserId,
            LastUpdate = entity.LastUpdate,
            DepartmentId = entity.DepartmentId,
            Data = new ApplicationDataModel
            {
                Description = entity.Data?.Description ?? string.Empty,
                Row = entity.Data?.Rows?.Select(MapRow).ToList() ?? []
            }
        };

    private static RowModel MapRow(RowEntity row)
        => new()
        {
            ID = row.ID,
            Name = row.Name,
            Description = row.Description,
            Style = row.Style,
            StyleRow = row.StyleRow,
            IsVisible = row.IsVisible,
            Attributes = row.Attributes?.Select(MapAttribute).ToList() ?? []
        };

    private static AttributeModel MapAttribute(AttributeBase attribute)
        => new()
        {
            ID = attribute.ID,
            AttributeType = attribute.AttributeType,
            Required = attribute.Required,
            Order = attribute.Order,
            Validation = attribute.Validation,
            Style = attribute.Style,
            Value = string.Empty
        };
}

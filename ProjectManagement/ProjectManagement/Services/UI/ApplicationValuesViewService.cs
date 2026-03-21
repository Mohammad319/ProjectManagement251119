using Application.Feature.Application.Commands;
using Application.Feature.Application.Queries;
using ProjectManagement.Client.Shared.Mapping;
using ProjectManagement.Client.Shared.Model.Application;

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
        return items?.Select(x => x.ToApplicationModel()).ToList() ?? [];
    }

    public Task<int> CreateAsync(ApplicationValuesModel model, CancellationToken ct = default)
        => dispatcher.Send(new CreateCalcAppCommand(model.ToApplicationValuesDto()), ct);

    public Task<bool> UpdateAsync(ApplicationValuesModel model, CancellationToken ct = default)
        => dispatcher.Send(new UpdateCalcAppCommand(model.ToApplicationValuesDto()), ct);
}

namespace Application.Feature.TfIdf;

public interface ITfIdfIndexProvider
{
    Task<Dictionary<string, double>> GetIdfScoresAsync(int tenantId, CancellationToken ct = default);
    void InvalidateAll();
}

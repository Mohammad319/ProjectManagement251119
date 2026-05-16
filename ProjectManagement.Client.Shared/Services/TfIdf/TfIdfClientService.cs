using ProjectManagement.Shared.Helper.Text;

namespace ProjectManagement.Client.Shared.Services.TfIdf;

/// <summary>
/// Client-side singleton (Scoped = singleton in WASM) that fetches the TF-IDF index
/// from the server once and caches it for the browser session.
/// </summary>
public sealed class TfIdfClientService
{
    private TfIdfIndex? _index;

    public TfIdfIndex Index => _index ?? TfIdfIndex.Empty;

    public bool IsLoaded => _index is not null;

    public void SetIndex(TfIdfIndex index) => _index = index;
}

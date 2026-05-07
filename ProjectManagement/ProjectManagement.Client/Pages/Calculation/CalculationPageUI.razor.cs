using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System.Threading;

namespace ProjectManagement.Client.Pages.Calculation
{
    public partial class CalculationPageUI : IAsyncDisposable
    {
        bool HeaderVisible { get; set; } = true;

        [Inject] private IClientLogger ClientLogger { get; set; } = default!;

        private HubConnection? hubConnection;
        private IDisposable? _calcSubscription;
        private bool _disposed;
        private int _joinedCalculationId;
        private int _observedCalculationId;
        private Action? _onFolderStateChanged;

        // Batching + debounce for bursts of hub events.
        private readonly object _hubBatchLock = new();
        private bool _batchStructuralDirty = false;
        private bool _batchAffectsCalc = false;

        private CancellationTokenSource? _hubFlushCts;
        private const int HubDebounceMs = 25;

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            lock (_hubBatchLock)
            {
                _hubFlushCts?.Cancel();
                _hubFlushCts?.Dispose();
                _hubFlushCts = null;
            }

            _calcSubscription?.Dispose();
            _calcSubscription = null;
            if (_onFolderStateChanged is not null)
                Folder.State.OnChange -= _onFolderStateChanged;

            if (hubConnection is not null)
            {
                hubConnection.Reconnected -= OnHubReconnected;
                await hubConnection.DisposeAsync();
                hubConnection = null;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            _onFolderStateChanged = () => _ = InvokeAsync(HandleCalculationChangedAsync);
            Folder.State.OnChange += _onFolderStateChanged;

            await HandleCalculationChangedAsync();
        }

        private async Task HandleCalculationChangedAsync()
        {
            if (_disposed)
                return;

            var calculation = Folder.State.Calculation;
            if (calculation is null || calculation.Id <= 0)
                return;

            if (_observedCalculationId != calculation.Id)
            {
                _observedCalculationId = calculation.Id;
                calculation.Opportunities = await Repo.Opportunity.GetAsync(calculation.Id);
            }

            await EnsureHubConnectionAsync();
            await JoinCurrentCalculationGroupAsync();
        }

        private async Task EnsureHubConnectionAsync()
        {
            if (_disposed || hubConnection is not null)
                return;

            hubConnection = new HubConnectionBuilder()
                .WithUrl(Navigation.ToAbsoluteUri("/notification"))
                .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(10)])
                .Build();

            _calcSubscription = hubConnection.On<ObjectTypHub, OperationType, object>("calc", OnHubEvent);
            hubConnection.Reconnected += OnHubReconnected;

            try
            {
                await hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
                hubConnection.Reconnected -= OnHubReconnected;
                _calcSubscription?.Dispose();
                _calcSubscription = null;
                await hubConnection.DisposeAsync();
                hubConnection = null;
                await ClientLogger.ErrorAsync("SignalR startup failed on calculation page", ex: ex);
            }
        }

        private async Task JoinCurrentCalculationGroupAsync()
        {
            var calculationId = Folder.State.Calculation?.Id ?? 0;
            if (_disposed || hubConnection is null || hubConnection.State != HubConnectionState.Connected || calculationId <= 0)
                return;

            if (_joinedCalculationId == calculationId)
                return;

            if (_joinedCalculationId > 0)
                await hubConnection.SendAsync("RemoveFromGroup", _joinedCalculationId);

            await hubConnection.SendAsync("AddToGroup", calculationId);
            _joinedCalculationId = calculationId;
        }

        private async Task OnHubReconnected(string? _)
        {
            try
            {
                _joinedCalculationId = 0;
                await JoinCurrentCalculationGroupAsync();
            }
            catch (Exception ex)
            {
                await ClientLogger.ErrorAsync("SignalR rejoin failed on calculation page", ex: ex);
            }
        }

        void OnHubEvent(ObjectTypHub typ, OperationType ot, object obj)
        {
            if (_disposed || obj == null)
                return;

            if (typ == ObjectTypHub.task)
                UoWService.Task.FromHub(ot, obj);
            else if (typ == ObjectTypHub.resource)
                UoWService.Resource.FromHub(ot, obj);
            else if (typ == ObjectTypHub.Offer)
                UoWService.Resource.FromOfferHub(ot, obj);
            else if (typ == ObjectTypHub.HourlyPrice)
                Calc.HourlyPriceList = obj.FromJsonWeb<List<HourlyPriceListGroupDTO>>();
            else if (typ == ObjectTypHub.Opportunity)
                CalcService.FromOperationHub(ot, obj);
            else if (typ == ObjectTypHub.calculation)
                CalcService.FromHub(ot, obj);

            bool structural =
                (typ == ObjectTypHub.task || typ == ObjectTypHub.resource || typ == ObjectTypHub.Offer) &&
                (ot == OperationType.Add || ot == OperationType.AddRange ||
                 ot == OperationType.Remove || ot == OperationType.RemoveRange ||
                 ot == OperationType.MoveRange);

            bool affectsCalc = Calc.LastHubChangeAffectsCalc;

            CancellationToken token;
            lock (_hubBatchLock)
            {
                _batchStructuralDirty |= structural;
                _batchAffectsCalc |= affectsCalc;

                _hubFlushCts?.Cancel();
                _hubFlushCts?.Dispose();

                _hubFlushCts = new CancellationTokenSource();
                token = _hubFlushCts.Token;
            }

            _ = InvokeAsync(async () =>
            {
                try
                {
                    await Task.Delay(HubDebounceMs, token);
                    await FlushHubBatchAsync();
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex)
                {
                    await ClientLogger.ErrorAsync("SignalR update flush failed on calculation page", ex: ex);
                }
            });
        }

        private async Task FlushHubBatchAsync()
        {
            if (_disposed)
                return;

            bool doStructural;
            bool doCalc;

            lock (_hubBatchLock)
            {
                doStructural = _batchStructuralDirty;
                doCalc = _batchAffectsCalc;

                _batchStructuralDirty = false;
                _batchAffectsCalc = false;
                Calc.LastHubChangeAffectsCalc = false;
            }

            if (doCalc)
                Calc.ExecuteCalculation();

            CalcService.RequestGridRefresh(doStructural
                ? CalculationGridRefreshKind.FlatList
                : CalculationGridRefreshKind.View);

            await InvokeAsync(StateHasChanged);
        }
    }
}

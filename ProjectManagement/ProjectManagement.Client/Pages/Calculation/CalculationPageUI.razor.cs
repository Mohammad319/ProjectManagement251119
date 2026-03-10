using Microsoft.AspNetCore.SignalR.Client;
using ProjectManagement.Shared.DTO.Calculation;
using System.Threading;

namespace ProjectManagement.Client.Pages.Calculation
{
    public partial class CalculationPageUI : IAsyncDisposable
    {
        bool HeaderVisible { get; set; } = true;

        private HubConnection? hubConnection;

        // ====== Batching + Debounce ======
        private readonly object _hubBatchLock = new();
        private bool _batchStructuralDirty = false;
        private bool _batchAffectsCalc = false;

        private CancellationTokenSource? _hubFlushCts;
        private const int HubDebounceMs = 25;   // يمكنك ضبطها بين 16 ~ 50 حسب الإحساس

        public async ValueTask DisposeAsync()
        {
            _hubFlushCts?.Cancel();
            _hubFlushCts?.Dispose();

            if (hubConnection is not null)
                await hubConnection.DisposeAsync();
        }

        protected override async Task OnInitializedAsync()
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl(Navigation.ToAbsoluteUri("/notification"), options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        return await Task.FromResult<string?>(null);
                    };
                })
                .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(10)])
                .Build();

            hubConnection.On<ObjectTypHub, OperationType, object>("calc", OnHubEvent);
            hubConnection.Reconnected += async _ =>
            {
                await AddToGroup();
            };

            await hubConnection.StartAsync();
            await AddToGroup();
            Calc.Opportunities = await Repo.Opportunity.GetAsync(Calc.Id);
        }

        private async Task AddToGroup()
            => await hubConnection?.SendAsync("AddToGroup", Calc.Id)!;

        void OnHubEvent(ObjectTypHub typ, OperationType ot, object obj)
        {
            if (obj == null) return;

            // ===== توزيع الحدث =====
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

            // ===== حساب نوع التغيير =====
            bool structural =
                (typ == ObjectTypHub.task || typ == ObjectTypHub.resource || typ == ObjectTypHub.Offer) &&
                (ot == OperationType.Add || ot == OperationType.AddRange ||
                 ot == OperationType.Remove || ot == OperationType.RemoveRange ||
                 ot == OperationType.MoveRange);

            bool affectsCalc = Calc.LastHubChangeAffectsCalc;

            CancellationToken token;
            // ===== تحديث flags + إعادة جدولة debounce =====
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
                    // تم إلغاء الدفعة بسبب وصول حدث أحدث
                }
            });
        }

        private async Task FlushHubBatchAsync()
        {
            bool doStructural;
            bool doCalc;

            lock (_hubBatchLock)
            {
                doStructural = _batchStructuralDirty;
                doCalc = _batchAffectsCalc;

                _batchStructuralDirty = false;
                _batchAffectsCalc = false;

                // مهم جدًا: صفّر الفلاج حتى لا “يلوث” الدفعة القادمة
                Calc.LastHubChangeAffectsCalc = false;
            }

            // ===== إعادة الحساب إذا لزم =====
            if (doCalc)
            {
                Calc.ExecuteCalculation();
            }

            // ===== إشعار الـGrid =====
            // structural => إعادة بناء FlatList
            // numeric فقط => RefreshDataAsync
            Calc.NotifyGridRefresh(flatListDirty: doStructural);

            // ===== إعادة رندر الصفحة =====
            await InvokeAsync(StateHasChanged);
        }
    }
}

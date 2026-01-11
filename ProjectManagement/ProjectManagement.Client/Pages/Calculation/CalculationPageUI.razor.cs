using Microsoft.AspNetCore.SignalR.Client;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Client.Pages.Calculation
{
    public partial class CalculationPageUI
    {
        bool HeaderVisible { get; set; } = true;

        private HubConnection? hubConnection;

        // ====== Batching ======
        private readonly object _hubBatchLock = new();
        private bool _batchScheduled = false;
        private bool _batchStructuralDirty = false;
        private bool _batchAffectsCalc = false;

        public async ValueTask DisposeAsync()
        {
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

            await hubConnection.StartAsync();
            await AddToGroup();
            Calc.Opportunities = await Repo.Opportunity.GetAsync(Calc.Id);
        }

        private async Task AddToGroup() => await hubConnection?.SendAsync("AddToGroup", Calc.Id);

        void OnHubEvent(ObjectTypHub typ, OperationType ot, object obj)
        {
            if (obj == null) return;

            // 1) طبّق التغيير على البيانات مباشرة (خفيف)
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

            // 2) حساب flags (هل بنيوي؟ هل رقمي؟)
            bool structural =
                (typ == ObjectTypHub.task || typ == ObjectTypHub.resource) &&
                (ot == OperationType.Add || ot == OperationType.AddRange ||
                 ot == OperationType.Remove || ot == OperationType.RemoveRange ||
                 ot == OperationType.MoveRange);

            bool affectsCalc = Calc.LastHubChangeAffectsCalc;

            // 3) اجمعهم ثم schedule flush واحد
            lock (_hubBatchLock)
            {
                _batchStructuralDirty |= structural;
                _batchAffectsCalc |= affectsCalc;

                if (!_batchScheduled)
                {
                    _batchScheduled = true;
                    _ = InvokeAsync(FlushHubBatchAsync);
                }
            }
        }

        private async Task FlushHubBatchAsync()
        {
            // نافذة تجميع صغيرة (تقلل الضغط في UI بشكل كبير)
            await Task.Delay(50);

            bool doStructural;
            bool doRecalc;

            lock (_hubBatchLock)
            {
                doStructural = _batchStructuralDirty;
                doRecalc = _batchAffectsCalc;

                _batchStructuralDirty = false;
                _batchAffectsCalc = false;
                _batchScheduled = false;

                // reset flag المصدر
                Calc.LastHubChangeAffectsCalc = false;
            }

            if (doRecalc)
            {
                // بما أنك تريد إعادة حساب كاملة:
                Calc.InvalidateAllCaches();
                Calc.ExecuteCalculation();
            }

            Calc.NotifyGridRefresh(flatListDirty: doStructural);
        }

        public bool IsConnected => hubConnection?.State == HubConnectionState.Connected;
    }
}

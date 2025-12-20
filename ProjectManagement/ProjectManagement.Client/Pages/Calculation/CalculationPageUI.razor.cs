using Microsoft.AspNetCore.SignalR.Client;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Client.Pages.Calculation
{
    public partial class CalculationPageUI
    {
        bool HeaderVisible { get; set; } = true;
        public async ValueTask DisposeAsync()
        {
            if (hubConnection is not null)
            {
                await hubConnection.DisposeAsync();
            }
        }

        private HubConnection? hubConnection;
        protected override async Task OnInitializedAsync()
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl(Navigation.ToAbsoluteUri("/notification"), options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        // ضع هنا طريقة جلب التوكن من التخزين/مزود التوكن عندك
                        // مثال: await tokenService.GetAccessTokenAsync();
                        return await Task.FromResult<string?>(null);
                    };
                })
                .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(10)])
                .Build();

            hubConnection.On<ObjectTypHub, OperationType, object>("calc", GetNewResource);
            await hubConnection.StartAsync();
            await AddToGroup();
            Calc.Opportunities = await Repo.Opportunity.GetAsync(Calc.Id);
        }
        private async Task AddToGroup() => await hubConnection?.SendAsync("AddToGroup", Calc.Id);

        void GetNewResource(ObjectTypHub typ, OperationType ot, object obj)
        {
            if (obj == null) return;
            else if (typ == ObjectTypHub.task)
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
            foreach (var ts in Calc.Tasks)
            {
                ts.InvalidateCache();
                foreach (var res in ts.Resources)
                    res.InvalidateCache();
            }
            Calc.ExecuteCalculation();
            Calc.RefreshCalculation();
        }
        public bool IsConnected => hubConnection?.State == HubConnectionState.Connected;
    }
}

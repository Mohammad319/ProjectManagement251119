using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;

namespace ProjectManagement.Client.Adminstrator.Shared.Culture
{
    public partial class CultureSelector
    {
        [Inject] public NavigationManager NavManager { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        readonly CultureInfo[] cultures = [new CultureInfo("en-US"), new CultureInfo("se-SE")];

        CultureInfo Culture
        {
            get => CultureInfo.CurrentCulture;
            set
            {
                if (CultureInfo.CurrentCulture != value)
                {
                    var js = (IJSInProcessRuntime)JSRuntime;
                    js.InvokeVoid("blazorCulture.set", value.Name);

                    NavManager.NavigateTo(NavManager.Uri, forceLoad: true);
                }
            }
        }
    }
}

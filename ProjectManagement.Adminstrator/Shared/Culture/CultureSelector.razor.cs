using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;

namespace ProjectManagement.Adminstrator.Shared.Culture
{
    public partial class CultureSelector
    {
        [Inject] public NavigationManager NavManager { get; set; } = default!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

        readonly CultureInfo[] cultures =
        [
            new CultureInfo("en-US"),
            new CultureInfo("sv-SE")
        ];

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

        private static string GetCultureLabel(CultureInfo culture)
            => culture.Name switch
            {
                "sv-SE" => "Svenska",
                "en-US" => "English",
                _ => culture.DisplayName
            };
    }
}

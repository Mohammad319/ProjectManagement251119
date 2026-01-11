using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace ProjectManagement.Client.Pages.Calculation.Table
{
    public partial class NelCalculationPage : IDisposable
    {
        private DotNetObjectReference<NelCalculationPage>? _dotNetRef;

        [Inject]
        private IJSRuntime JS { get; set; } = default!;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("initializeResizableColumns", _dotNetRef);
            }
        }

        [JSInvokable]
        public async Task SaveTemplateBlazor(string item)
        {
            try
            {
                Template.StyleNetCalc = string.Empty;

                // item: "columnIndex||newWidth"
                string[] arr = item.Split("||", StringSplitOptions.RemoveEmptyEntries);
                if (arr.Length < 2) return;

                int headerIndex = int.Parse(arr[0]);
                int newWidth = int.Parse(arr[1]);

                // إذا أردت تطبيق width على Template.NetCalc.Columns هنا أضف منطقك
                // Template.NetCalc.Columns.FirstOrDefault(c => c.Id == headerIndex)?.Width = newWidth;

                if (Calc?.TemplateId > 0)
                {
                    Template.StyleNetCalc = "";
                    TemplateListPostDTO temp = new();
                    Template.CopyPropertiesTo(temp);
                    await Repo.Template.UpdateAsync(temp, Calc.TemplateId.Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " Template has been not updated");
            }
        }

        protected override void OnInitialized()
        {
            // اجعل الجدول هو المسؤول عن بناء FlatList عند أول تحميل
            Calc.FlatListDirty = true;
        }

        public void Dispose()
        {
            _dotNetRef?.Dispose();
        }
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ProjectManagement.Shared.Constant;
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
                if (string.IsNullOrWhiteSpace(item))
                    return;

                string[] arr = item.Split("||", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (arr.Length < 2 ||
                    !int.TryParse(arr[0], out int headerIndex) ||
                    !int.TryParse(arr[1], out int newWidth))
                {
                    return;
                }

                newWidth = Math.Max(PMValuesConst.MinWidthCol, newWidth);

                if (headerIndex == 1)
                {
                    Template.StartCol1 = newWidth;
                }
                else
                {
                    int templateColumnIndex = headerIndex - 2;
                    if (templateColumnIndex < 0 || templateColumnIndex >= Template.NetCalc.Columns.Count)
                        return;

                    Template.NetCalc.Columns[templateColumnIndex].Width = newWidth;
                }

                Template.FreezCol();
                await InvokeAsync(StateHasChanged);

                if (Calc?.TemplateId > 0)
                {
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

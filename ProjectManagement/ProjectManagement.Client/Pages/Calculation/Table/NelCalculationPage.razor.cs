using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace ProjectManagement.Client.Pages.Calculation.Table
{
    public partial class NelCalculationPage : IDisposable
    {

        // مرجع .NET الذي سيتم تمريره إلى JavaScript
        private DotNetObjectReference<NelCalculationPage>? _dotNetRef;

        [Inject]
        IJSRuntime JS { get; set; } = default!;

        // NOTE:
        // الكود يفترض أن الخصائص/الحقول التالية معرفة في نفس المكوّن (في الجزء الآخر من partial أو في نفس الملف):
        // - Template  (كائن يحتوي على NetOrder, NetWidth, StyleNetCalc, CopyPropertiesTo, ...)
        // - Calc      (كائن يحتوي على TemplateId, Tasks, AllFlatItems, BuildFlatList, ...)
        // - Repo      (يحتوي على Template.UpdateAsync)
        // لو لم تكن موجودة، أبقها كما كانت عندك سابقاً أو عرّفها كما في مشروعك.

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // إنشاء مرجع .NET وتمريره إلى JavaScript
                _dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("initializeResizableColumns", _dotNetRef);
            }
        }

        // هذه الدالة يستدعيها JavaScript:
        // window.nelCalcDotNetRef.invokeMethodAsync('SaveTemplateBlazor', event);
        [JSInvokable]
        public async Task SaveTemplateBlazor(string item)
        {
            try
            {
                Template.StyleNetCalc = string.Empty;

                // item بالشكل: "columnIndex||newWidth"
                string[] arr = item.Split("||", StringSplitOptions.RemoveEmptyEntries);
                if (arr.Length < 2) return;

                int headerIndex = int.Parse(arr[0]);
                int newWidth = int.Parse(arr[1]);
                // حساب رقم العمود الفعلي حسب NetOrder
                //int colIndex = Template.NetOrder[headerIndex - 2];
               // Template.NetWidth[colIndex] = newWidth;
                Template?.NetColumnsToUse?.FirstOrDefault(c => c.Id == headerIndex)?.Width = newWidth;
                if (Calc?.TemplateId > 0)
                {
                    Template?.StyleNetCalc = "";
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
            // منطق التهيئة كما كان عندك
            Calc.AllFlatItems = Calc.BuildFlatList(Calc.Tasks.Where(x => x.TaskId == null));
            if (Calc.AllFlatItems.Count > 0)
                Template.StartCol1 = (Calc.AllFlatItems.Max(x => x.Depth) * 10) + 15;
        }

        public void Dispose()
        {
            _dotNetRef?.Dispose();
        }
    }
}
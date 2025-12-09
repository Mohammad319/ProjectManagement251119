using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Adminstrator.Shared.Error
{
    public class CustomErrorBoundary: ErrorBoundary
    {
        //[Inject] private IWebAssemblyHostEnvironment env { get; set; }
        //protected override ParentTask OnErrorAsync(System.Exception exception)
        //{
        //    if(env.IsDevelopment())
        //        return base.OnErrorAsync(exception);
        //    return ParentTask.CompletedTask;
        //}
    }
}

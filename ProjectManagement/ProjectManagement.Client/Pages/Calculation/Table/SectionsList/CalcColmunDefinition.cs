using Microsoft.AspNetCore.Components.Rendering;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList
{
    public record CalcColmunDefinition<TTask, TRes>
    {
        public NetColumnId Id { get; set; }
        public Action<RenderTreeBuilder, TTask> TaskRender { get; init; } = static (_, _) => { };
        public Action<RenderTreeBuilder, TRes> ResRender { get; init; } = static (_, _) => { };
    }
}

using Microsoft.AspNetCore.Components;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList
{
    public record CalcColmunDefinition<TTask, TRes>
    {
        public string? Title { get; set; }
        public Func<TTask, RenderFragment> TaskRender { get; init; } = _ => __builder => { };
        public Func<TRes, RenderFragment> ResRender { get; init; } = _ => __builder => { };
    }
}

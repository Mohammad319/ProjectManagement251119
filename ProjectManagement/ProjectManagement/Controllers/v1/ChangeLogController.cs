using Application.Feature.ChangeLog.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectManagement.Server.Controllers.v1
{
    /// <summary>
    /// Recent change-log entries for the project/calculation "ändringsindikator" tooltip.
    /// The caller posts the ids it already loaded (and is therefore allowed to see); results are
    /// tenant-scoped via the global query filter. Read-only, low-sensitivity metadata.
    /// </summary>
    [ApiVersion("1.0")]
    [Authorize]
    public class ChangeLogController : BaseApiController
    {
        [HttpPost("projects")]
        public async Task<IActionResult> RecentForProjects([FromBody] IReadOnlyList<Guid>? ids, [FromQuery] int take = 5)
            => Ok(await MicroBus.Send(new GetRecentProjectChangesQuery(ids ?? [], take)));

        [HttpPost("calculations")]
        public async Task<IActionResult> RecentForCalculations([FromBody] IReadOnlyList<int>? ids, [FromQuery] int take = 5)
            => Ok(await MicroBus.Send(new GetRecentCalculationChangesQuery(ids ?? [], take)));
    }
}

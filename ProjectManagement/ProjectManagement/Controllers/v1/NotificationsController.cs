using Application.Feature.Notification.Commands;
using Application.Feature.Notification.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectManagement.Server.Controllers.v1
{
    /// <summary>
    /// Per-user notifications. Every endpoint is scoped to the authenticated user (taken from claims,
    /// never from the request body) so a user can only ever see/affect their own notifications.
    /// </summary>
    [ApiVersion("1.0")]
    [Authorize]
    public class NotificationsController : BaseApiController
    {
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] int take = 15)
            => Ok(await MicroBus.Send(new GetNotificationSummaryQuery(GetUserId(), take)));

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int skip = 0, [FromQuery] int take = 30)
            => Ok(await MicroBus.Send(new GetNotificationsQuery(GetUserId(), skip, take)));

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
            => Ok(await MicroBus.Send(new GetUnreadNotificationCountQuery(GetUserId())));

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
            => Ok(await MicroBus.Send(new MarkNotificationReadCommand(id, GetUserId())));

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
            => Ok(await MicroBus.Send(new MarkAllNotificationsReadCommand(GetUserId())));

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
            => Ok(await MicroBus.Send(new DeleteNotificationCommand(id, GetUserId())));

        [HttpPost("clear-read")]
        public async Task<IActionResult> ClearRead()
            => Ok(await MicroBus.Send(new ClearReadNotificationsCommand(GetUserId())));

        [HttpGet("open-info/{projectId:guid}")]
        public async Task<IActionResult> OpenInfo(Guid projectId)
            => Ok(await MicroBus.Send(new GetProjectOpenInfoQuery(projectId, GetUserId(), GetDepartmentId(), IsViewer())));
    }
}

using System;

namespace ProjectManagement.Shared.Exceptions
{
    /// <summary>
    /// Thrown when an authenticated user attempts an action they are not permitted to perform on a
    /// resource they CAN otherwise see (e.g. a calculation reached via extra project sharing, which
    /// grants work access but not lifecycle management). Mapped to HTTP 403 with the message shown to
    /// the user, so the message must be safe, user-facing text.
    /// </summary>
    public sealed class ForbiddenActionException : Exception
    {
        public ForbiddenActionException(string message) : base(message) { }
    }
}

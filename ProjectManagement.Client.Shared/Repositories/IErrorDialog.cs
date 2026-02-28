#nullable disable
﻿namespace ProjectManagement.Client.Shared.Repositories
{
    public interface IErrorDialog
    {
        void Show(string title, string message, string traceId = null);
    }
}

using ProjectManagement.Client.Shared.Exception;
using System;

namespace ProjectManagement.Client.Handless
{
    public interface ILogger
    {
        /// <summary>
        /// يسجل استثناء عام.
        /// </summary>
        void Log(Exception ex);

        /// <summary>
        /// يسجل استثناء خاص باستجابة HTTP.
        /// </summary>
        void Log(HttpResponseException ex);
    }
}

namespace ProjectManagement.Adminstrator.Handless
{
    public interface ILoggerPM
    {
        /// <summary>
        /// يسجل استثناء عام.
        /// </summary>
        void Log(Exception ex);

        /// <summary>
        /// يسجل استثناء خاص باستجابة HTTP.
        /// </summary>
    }
}

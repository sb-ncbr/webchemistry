namespace WebChemistry.Platform
{
    using System;

    /// <summary>
    /// Date time utils.
    /// </summary>
    public static class DateTimeService
    {
        /// <summary>
        /// Get the current time (UTC).
        /// </summary>
        /// <returns></returns>
        public static DateTime GetCurrentTime()
        {
            return DateTime.UtcNow;
        }
    }
}

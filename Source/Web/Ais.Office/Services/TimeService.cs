namespace Ais.Office.Services
{
    using Ais.Data.Common.Base;
    using Ais.Utilities.Helpers;

    /// <summary>
    /// Class TimeService.
    /// Implements the <see cref="ITimeService" />
    /// </summary>
    /// <seealso cref="ITimeService" />
    public class TimeService : ITimeService
    {
        /// <summary>
        /// Gets the current time.
        /// </summary>
        /// <returns>DateTime.</returns>
        public DateTime GetCurrentTime()
        {
            return AsyncHelper.RunSync(this.GetCurrentTimeAsync);
        }

        /// <summary>
        /// Gets the current time asynchronous.
        /// </summary>
        /// <returns>Task&lt;DateTime&gt;.</returns>
        public Task<DateTime> GetCurrentTimeAsync()
        {
            // TODO - this will be web client with async callback
            return Task.FromResult(DateTime.UtcNow);
        }
    }
}

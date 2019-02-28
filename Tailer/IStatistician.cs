using System.Collections.Generic;

namespace Tailer
{
    /// <summary>
    /// Defines a tool which can calculate statistics about lines of text
    /// </summary>
    public interface IStatistician
    {
        /// <summary>
        /// The user configuration options
        /// </summary>
        IConfiguration Configuration { get; }

        /// <summary>
        /// Process one line and return a boolean indicating if the line passes the interval
        /// </summary>
        /// <param name="line"></param>
        /// <returns>True if the timestamps in the log pass into a new interval</returns>
        bool Update(string line);

        /// <summary>
        /// Calculate additional statistics based on recent lines
        /// </summary>
        /// <returns>A statistics dictionary</returns>
        Statistics GetStatistics();
    }
}
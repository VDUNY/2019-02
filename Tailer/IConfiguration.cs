namespace Tailer
{
    public interface IConfiguration
    {
        /// <summary>
        /// The path of the file being monitored
        /// </summary>
        string Path { get; }

        /// <summary>
        /// When false, the program processes the entire current log file and then additional lines as they're added
        /// When true, the program skips current content and starts at the end
        /// </summary>
        bool SkipContent { get; }

        /// <summary>
        /// The reporting interval between statistics output
        /// </summary>
        int Interval { get; }

        /// <summary>
        /// The number of seconds of traffic to consider for threshold limits
        /// </summary>
        int Window { get; }

        /// <summary>
        /// The threshold limit
        /// </summary>
        int Threshold { get; }
    }
}
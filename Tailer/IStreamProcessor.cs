using System.IO;
using Microsoft.Extensions.Logging;

namespace Tailer
{
    /// <summary>
    /// An interface for objects which process streams
    /// </summary>
    public interface IStreamProcessor
    {
        /// <summary>
        /// This method will be called to process a stream, starting at offset
        /// </summary>
        /// <param name="stream">The stream to process</param>
        /// <param name="offset">The offset to start at</param>
        void Process(Stream stream, long offset);

        /// <summary>
        /// The configuration required for the stream processor
        /// </summary>
        IConfiguration Configuration { get; }

        /// <summary>
        /// A tool which can calculate statistics about the stream
        /// </summary>
        IStatistician Statistician { get; }

        /// <summary>
        /// Where output from the stream processor should go
        /// </summary>
        ILogger<TailProcessor> Output { get; }
    }
}
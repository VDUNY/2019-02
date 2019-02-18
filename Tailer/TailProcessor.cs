using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Tailer
{
    public class TailProcessor : IStreamProcessor
    {
        private readonly EventId[] outputEvents =
        {
            new EventId(0, "Normal Traffic"),
            new EventId(1, "High Traffic"),
            new EventId(2, "Statistics"),
            new EventId(3, "Bad Data"),
        };

        private bool alerted = false;
        private DateTime lastReport = DateTime.Now;

        public IConfiguration Configuration { get; }
        public IStatistician Statistician { get; }
        public ILogger<TailProcessor> Output { get; }

        public TailProcessor(IConfiguration handler, ILogger<TailProcessor> output, IStatistician statistician)
        {
            Configuration = handler;
            Statistician = statistician;
            Output = output;
        }

        /// <summary>
        /// This method will be called to process a stream, starting at offset
        /// </summary>
        /// <param name="stream">The stream to process</param>
        /// <param name="offset">The offset to start at</param>
        public void Process(Stream stream, long offset)
        {
            Process(stream, offset, 0);
        }

        /// <summary>
        /// This method is a test overload which allows the Process method to stop when it gets past a certain offset in the stream
        /// </summary>
        /// <param name="stream">The stream to process</param>
        /// <param name="offset">The offset to start at</param>
        /// <param name="maxOffset">The furthest offset to process</param>
        public void Process(Stream stream, long offset, long maxOffset)
        {
            using (var reader = new StreamReader(stream))
            {
                // fast-forward to the the current offset
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);

                // loop forever (except in test cases where maxOffset was passed in)
                while (maxOffset == 0 || offset < maxOffset)
                {
                    // idle if there's nothing new
                    if (reader.BaseStream.Length == offset)
                    {
                        Thread.Sleep(100);
                        // If we haven't seen logs for awhile, we need to show the stats we have
                        var now = DateTime.Now;
                        if ((now - lastReport).Seconds > Configuration.Interval)
                        {
                            ShowStats();
                        }

                        continue;
                    }

                    // read lines out of the file until there are no more
                    var line = "";
                    while ((line = reader.ReadLine()) != null)
                    {
                        try
                        {
                            if (Statistician.Update(line))
                            {
                                ShowStats();
                            }
                        }
                        catch (InvalidDataException)
                        {
                            Output.LogError(outputEvents[3], "Unable to parse log line: {line}", line);
                        }
                    }

                    // bookmark where we've read to
                    offset = reader.BaseStream.Position;
                }
            }
        }

        public void ShowStats()
        {
            // make sure we don't trigger on time while there's a chance we'll trigger on log content
            lastReport = DateTime.Now.AddMilliseconds(750);

            var stats = Statistician.GetStatistics();

            if (alerted != stats.ThresholdExceeded)
            {
                if (stats.ThresholdExceeded)
                {
                    Output.LogWarning(outputEvents[1], "High traffic generated an alert - hits = {WindowAverage}/sec, triggered at {DateTime}", stats.WindowAverage, DateTimeOffset.UtcNow);
                }
                else
                {
                    Output.LogWarning(outputEvents[0], "High traffic alert recovered - hits = {WindowAverage}/sec, recovered at {DateTime}", stats.WindowAverage, DateTimeOffset.UtcNow);
                }

                alerted = stats.ThresholdExceeded;
            }

            var message = $"Last {Configuration.Interval} seconds:".PadRight(18) +
                          $"{stats.TotalRequests} hits ({stats.RequestsPerSecond}/sec). Average over {Configuration.Window} seconds: {stats.WindowAverage}/sec.";

            foreach (var stat in stats.Reports.Keys.Where(stat => stats.Reports[stat].Count > 0))
            {
                message += $"\n{stat}:".PadRight(19);
                message += string.Join(", ", stats.Reports[stat].Keys.Select(n => $"{n}: {stats.Reports[stat][n]}"));
            }

            message += "\n";

            Output.LogInformation(outputEvents[2], message);
        }
    }
}

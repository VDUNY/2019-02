using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Tailer
{
    /// <summary>
    /// An implementation of ILineHandler that maintains stats
    /// </summary>
    public class W3CStats : IStatistician
    {
        // cache the actual log lines for each statistics interval (clear this whenever the stats are read)
        private readonly List<W3CLine> log = new List<W3CLine>();

        // we keep the count as an array of counts, with each bucket being 'Interval' seconds
        private DateTimeOffset last = DateTimeOffset.MinValue;
        private int rollingIndex = 0;
        private int hitCounter = 0;
        private readonly int[] rollingCount;

        public IConfiguration Configuration { get; }

        /// <summary>
        /// Create an instance using the specified configuration
        /// </summary>
        /// <param name="configuration">The configuration for these stats</param>
        public W3CStats(IConfiguration configuration)
        {
            Configuration = configuration;

            // we need enough buckets to store the window in
            // i.e. if the window is 2 minutes, and we report 10 second intervals, we need 120/10 = 12 buckets
            rollingCount = new int[Configuration.Window / Configuration.Interval];
        }

        /// <inheritdoc />
        public bool Update(string line)
        {
            bool update = false;
            var data = new W3CLine(line);

            if (last == DateTimeOffset.MinValue)
            {
                last = data.Time;
            }

            // Add the counter to a bucket at the end of each interval
            // This means we only trip alerts at the end of the interval
            if ((data.Time - last).TotalSeconds >= Configuration.Interval)
            {
                CalculateStatistics(log);
                rollingIndex = (++rollingIndex) % rollingCount.Length;
                hitCounter = 0;
                log.Clear();

                last = data.Time;
                update = true;
            }

            hitCounter++;
            log.Add(data);

            return update;
        }

        /// <summary>
        /// A cached copy of the latest stats that gets cleared when read
        /// </summary>
        private Statistics? stats = null;

        /// <summary>
        /// Calculate (and cache) the statistics on the log
        /// </summary>
        /// <param name="lines">A collection of <see cref="W3CLine"/></param>
        private void CalculateStatistics(List<W3CLine> lines)
        {
            rollingCount[rollingIndex] = hitCounter;

            var average = 0;

            if (lines.Count > 0)
            {
                average = (int) Math.Ceiling(lines.Count / (1 + (lines.Last().Time - lines.First().Time).TotalSeconds));
            }
            var window = (int) Math.Ceiling((rollingCount.Sum()) / (double) Configuration.Window);

            stats = new Statistics(lines.Count, average, window, window > Configuration.Threshold);
            stats.Value.Reports.Add("Top Sections", lines.GroupBy(line => line.Request.Path)
                                                   .OrderByDescending(g => g.Count())
                                                   .Take(10).ToDictionary(data => data.Key, data => data.Count()));

            stats.Value.Reports.Add("Top Users", lines.GroupBy(line => line.AuthenticatedUser)
                                                .OrderByDescending(g => g.Count())
                                                .Take(10).ToDictionary(data => data.Key, data => data.Count()));

            stats.Value.Reports.Add("Top Errors", lines.Where(line => line.Status >= 400).GroupBy(line => line.Status)
                                                 .OrderByDescending(g => g.Count())
                                                 .Take(10).ToDictionary(data => data.Key.ToString(), data => data.Count()));

            stats.Value.Reports.Add("Problem Sections", lines.Where(line => line.Status >= 400)
                                                       .GroupBy(line => line.Request.Path)
                                                       .OrderByDescending(g => g.Count())
                                                       .Take(10).ToDictionary(data => data.Key.ToString(), data => data.Count()));

        }

        /// <inheritdoc />
        public Statistics GetStatistics()
        {
            // Support calling this from elapsed log time, or wall time
            // 1. Update() determines Interval seconds have passed and returns true
            // 2. The caller determines we've been idle for Interval seconds and calls this method
            //
            // In the second case, the stats will be null, and we'll calculate based on the current log
            if (!stats.HasValue)
            {
                CalculateStatistics(log);
                log.Clear();
            }

            // Return the cached stats, and clear the cache
            var report = stats;
            stats = null;

            Debug.Assert(report != null, nameof(report) + " != null");
            return report.Value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NLog;

namespace Tailer
{
    public struct Statistics
    {
        public int TotalRequests { get; set; }
        public int RequestsPerSecond { get; set; }
        public int WindowAverage { get; set; }
        public bool ThresholdExceeded { get; set; }

        public Dictionary<string, Dictionary<string, int>> Reports { get; set; }

        public Statistics(int totalRequests, int requestsPerSecond, int windowAverage, bool thresholdExceeded)
        {
            TotalRequests = totalRequests;
            RequestsPerSecond = requestsPerSecond;
            WindowAverage = windowAverage;
            ThresholdExceeded = thresholdExceeded;
            Reports = new Dictionary<string, Dictionary<string, int>>();
        }
    }
}

using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Tailer
{
    public struct W3CLine
    {
        private static readonly Regex parser = new Regex(@"^(?<remote>\S+) (?<rfcuser>\S+) (?<authuser>\S+) \[(?<date>[^\]]+)\] ""(?<request>(?<verb>\w+) /(?<path>[^/]+)(/?(?<page>\S+))? (?<transport>.*))"" (?<status>\d+) (?<bytes>\d+)", RegexOptions.Compiled);

        public string RemoteClient;
        public string AuthenticatedUser;
        public DateTimeOffset Time;
        public Request Request;
        public int Status;
        public long Bytes;

        public W3CLine(string line)
        {
            var data = parser.Match(line);
            if (!data.Success)
            {
                throw new InvalidDataException("Can't parse line: " + line);
            }

            RemoteClient = data.Groups["remote"].Value;
            AuthenticatedUser = data.Groups["authuser"].Value;
            Time = DateTimeOffset.ParseExact(data.Groups["date"].Value, "dd/MMM/yyyy:HH:mm:ss zzz", CultureInfo.InvariantCulture.DateTimeFormat);
            Request = new Request(data.Groups["request"].Value, data.Groups["verb"].Value, data.Groups["path"].Value, data.Groups["page"].Value);
            Status = int.Parse(data.Groups["status"].Value);
            Bytes = long.Parse(data.Groups["bytes"].Value);
        }

    }
}
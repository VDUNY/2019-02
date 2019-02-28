using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Autofac.Extras.Moq;
using NLog.LayoutRenderers.Wrappers;
using NUnit.Framework;

namespace Tailer.Tests
{
    public static class RandomHelper
    {
        private static readonly Random pick = new Random();

        public static string Random(this string[] array)
        {
            return array.OrderBy(x => pick.Next()).First();
        }
    }

    [TestFixture]
    public class W3CStatsTests
    {
        const string template = "127.0.0.1 - {0} [{1:dd/MMM/yyyy:HH:mm:ss zzz}] \"GET {2} HTTP/1.0\" 200 100";
        readonly string[] users = { "alex", "bob", "evan", "frank", "hannah", "james", "jill", "john", "mark", "steve", "tina", "veronica" };
        readonly string[] urls = { "/api/user", "/report", "/user/steve", "/user/frank", "/api/posts", "/api/post/1", "/blog/1", "/blog/2", "/api/post/2", "/user/tina", "/user/veronica", "/user/alex" };

        private IEnumerable<string> GetRandomLog(DateTimeOffset offset, int seconds = 120, int rate = 10)
        {
            for (var t = 0; t < seconds; t++)
            {
                for (var i = 0; i < rate; i++)
                {
                    yield return string.Format(template, users.Random(), offset, urls.Random());
                }

                offset = offset.AddSeconds(1);
            }
        }

        private IEnumerable<string> GetConstantLog(int seconds = 120, int rate = 10)
        {
            var now = DateTimeOffset.UtcNow;

            for (var t = 0; t < seconds; t++)
            {
                for (var i = 0; i < rate; i++)
                {
                    yield return string.Format(template, users[i % users.Length], now, urls[i % urls.Length]);
                }

                now = now.AddSeconds(1);
            }
        }

        [Test]
        public void Returns_True_Only_When_The_Threshold_Is_Exceeded()
        {
            using (var mock = AutoMock.GetLoose())
            {
                mock.Mock<IConfiguration>().Setup(c => c.Interval).Returns(10);
                mock.Mock<IConfiguration>().Setup(c => c.Threshold).Returns(5);
                mock.Mock<IConfiguration>().Setup(c => c.Window).Returns(120);

                var sut = mock.Create<W3CStats>();
                var start = DateTimeOffset.UtcNow;
                Statistics stats;

                // A full two minutes of data with exactly threshold hits:
                var fullLog = GetRandomLog(start, 120, 5).ToArray();
                foreach (var logLine in fullLog)
                {
                    if (!sut.Update(logLine)) continue;

                    stats = sut.GetStatistics();
                    Assert.LessOrEqual(stats.WindowAverage, 5);
                }

                // But if the next interval goes over:
                fullLog = GetRandomLog(start.AddSeconds(121), 10, 6).ToArray();
                foreach (var logLine in fullLog)
                {
                    if (!sut.Update(logLine)) continue;
                    sut.GetStatistics();
                }

                stats = sut.GetStatistics();
                Assert.Greater(stats.WindowAverage, 5);
            }
        }


        [Test]
        public void Extended_Traffic_Test()
        {
            using (var mock = AutoMock.GetLoose())
            {
                mock.Mock<IConfiguration>().Setup(c => c.Interval).Returns(10);
                mock.Mock<IConfiguration>().Setup(c => c.Threshold).Returns(5);
                mock.Mock<IConfiguration>().Setup(c => c.Window).Returns(30);

                var sut = mock.Create<W3CStats>();

                Statistics stats;
                var start = DateTimeOffset.UtcNow;

                // First, fill the Window with just enough to not trip the threshold
                var fullLog = GetRandomLog(start, 30, 5);

                foreach (var logLine in fullLog)
                {
                    if (!sut.Update(logLine)) continue;

                    stats = sut.GetStatistics();
                    Assert.LessOrEqual(stats.WindowAverage, 5);
                }

                // Now, go over the threshold, and it will instantly trigger
                fullLog = GetRandomLog(start.AddSeconds(30), 10, 6);

                foreach (var logLine in fullLog)
                {
                    if (!sut.Update(logLine)) continue;
                    stats = sut.GetStatistics();
                }
                // we stopped sending logs on the 10-second boundary, so we need to read it by hand
                stats = sut.GetStatistics();
                Assert.Greater(stats.WindowAverage, 5);

                // Now go back below the threshold. It should fall down below after half the time
                fullLog = GetRandomLog(start.AddSeconds(40), 10, 4);

                foreach (var logLine in fullLog)
                {
                    if (!sut.Update(logLine)) continue;
                    stats = sut.GetStatistics();
                }
                // we stopped sending logs on the 10-second boundary, so we need to read it by hand
                stats = sut.GetStatistics();
                Assert.LessOrEqual(stats.WindowAverage, 5);
            }
        }



        [Test]
        public void Calculates_Statistics_Correctly()
        {
            using (var mock = AutoMock.GetLoose())
            {
                mock.Mock<IConfiguration>().Setup(c => c.Interval).Returns(10);
                mock.Mock<IConfiguration>().Setup(c => c.Threshold).Returns(10);
                mock.Mock<IConfiguration>().Setup(c => c.Window).Returns(10);

                var sut = mock.Create<W3CStats>();

                var fullLog = GetConstantLog(sut.Configuration.Window, sut.Configuration.Threshold).ToArray();
                foreach (var logLine in fullLog)
                {
                    var result = sut.Update(logLine);
                    Assert.IsFalse(result);
                }

                // calculate the statistics
                var statistics = sut.GetStatistics();

                Assert.AreEqual(fullLog.Length, statistics.TotalRequests);
                Assert.AreEqual(fullLog.Length / 10, statistics.RequestsPerSecond);

                // In every 10 items, there should be 4 /api, 3 /user, 2 /blog and 1 /report
                Assert.Greater(statistics.Reports["Top Sections"]["api"], statistics.Reports["Top Sections"]["user"]);
                Assert.Greater(statistics.Reports["Top Sections"]["user"], statistics.Reports["Top Sections"]["blog"]);

                // basically, there are going to be ten users with the same number of hits
                Assert.AreEqual(statistics.Reports["Top Users"]["alex"], statistics.Reports["Top Users"]["bob"]);
                Assert.AreEqual(statistics.Reports["Top Users"]["evan"], statistics.Reports["Top Users"]["frank"]);
            }
        }
    }
}
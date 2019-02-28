using System;
using System.Collections.Generic;
using System.IO;
using Autofac.Extras.Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Moq;
using NUnit.Framework;

namespace Tailer.Tests
{
    [TestFixture]
    public class TailProcessorTests
    {
        public static Stream GetStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        [Test]
        public void Process_Calls_Statistician_With_Each_Line()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var sut = mock.Create<TailProcessor>();

                mock.Mock<IStatistician>().Setup(s => s.Update(It.IsAny<string>()));

                using (var stream = GetStreamFromString(
                    "Line Zero\n" +
                    "Line One\n" +
                    "Line Two\n"))
                {
                    sut.Process(stream, 0, stream.Length);
                }

                mock.Mock<IStatistician>().Verify(s => s.Update("Line Zero"));
                mock.Mock<IStatistician>().Verify(s => s.Update("Line One"));
                mock.Mock<IStatistician>().Verify(s => s.Update("Line Two"));
            }
        }

        [Test]
        public void Process_Skips_Lines_Before_The_Offset()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var sut = mock.Create<TailProcessor>();

                mock.Mock<IStatistician>().Setup(s => s.Update(It.IsAny<string>()));

                using (var stream = GetStreamFromString(
                    "Line Zero\n" +
                    "Line One\n" +
                    "Line Two\n"))
                {
                    sut.Process(stream, "Line Zero\n".Length, stream.Length);
                }

                mock.Mock<IStatistician>().Verify(s => s.Update("Line Zero"), Times.Never);
                mock.Mock<IStatistician>().Verify(s => s.Update("Line One"));
                mock.Mock<IStatistician>().Verify(s => s.Update("Line Two"));
            }
        }

        [Test]
        public void Process_Calls_GetStatistics_When_Statistician_Update_Is_True()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var sut = mock.Create<TailProcessor>();
                // Mock statistician so the alert is never set
                mock.Mock<IStatistician>().Setup(s => s.Update(It.IsAny<string>())).Returns(true).Verifiable();
                mock.Mock<IStatistician>().Setup(s => s.GetStatistics()).Returns(new Statistics(50, 5, 5, false)).Verifiable();

                using (var stream = GetStreamFromString("Line Zero\n"))
                {
                    sut.Process(stream, 0, stream.Length);
                }

                mock.Mock<IStatistician>().Verify();
            }
        }

        [Test]
        public void Process_Does_Not_Log_An_Alert_When_The_Status_Is_False()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var sut = mock.Create<TailProcessor>();

                // Mock statistician so the alert is never set
                mock.Mock<IStatistician>().Setup(s => s.Update(It.IsAny<string>())).Returns(true).Verifiable();
                mock.Mock<IStatistician>().Setup(s => s.GetStatistics()).Returns(new Statistics(50, 5, 5, false)).Verifiable();

                // Neither of these should ever be called, so they throw exceptions if they are
                mock.Mock<ILogger<TailProcessor>>().Setup(x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<FormattedLogValues>(v => v.ToString().StartsWith("High traffic generated an alert")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<object, Exception, string>>())).Throws(new InvalidDataException("There was no high traffic"));

                mock.Mock<ILogger<TailProcessor>>().Setup(x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<FormattedLogValues>(v => v.ToString().StartsWith("High traffic alert recovered")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<object, Exception, string>>())).Throws(new InvalidDataException("There was no high traffic"));


                using (var stream = GetStreamFromString(
                    "Line Zero\n" +
                    "Line One\n" +
                    "Line Two\n"))
                {
                    sut.Process(stream, 0, stream.Length);
                }

                mock.Mock<IStatistician>().Verify();
            }
        }


        [Test]
        public void Process_Logs_An_Alert_When_The_Result_Changes()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var sut = mock.Create<TailProcessor>();

                // Mock statistician so the alert flips back and forth
                mock.Mock<IStatistician>().Setup(s => s.Update(It.IsAny<string>())).Returns(true);
                mock.Mock<IStatistician>().Setup(s => s.GetStatistics()).Returns(
                    new Queue<Statistics>(new[] {
                        new Statistics(100, 5, 10, true),
                        new Statistics(2, 5, 5, false),
                        new Statistics(100, 5, 10, true)
                    }).Dequeue
                );


                // We expect both of these to be called
                mock.Mock<ILogger<TailProcessor>>().Setup(x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<FormattedLogValues>(v => v.ToString().StartsWith("High traffic generated an alert")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<object, Exception, string>>()));

                mock.Mock<ILogger<TailProcessor>>().Setup(x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<FormattedLogValues>(v => v.ToString().StartsWith("High traffic alert recovered")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<object, Exception, string>>()));


                using (var stream = GetStreamFromString(
                    "Line Zero\n" +
                    "Line One\n" +
                    "Line Two\n"))
                {
                    sut.Process(stream, 0, stream.Length);
                }

                mock.Mock<ILogger<TailProcessor>>().VerifyAll();
            }
        }
    }
}
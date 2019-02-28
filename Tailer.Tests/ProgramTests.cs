using System.IO;
using System.Linq;
using Autofac;
using Autofac.Extras.Moq;
using Microsoft.Extensions.Logging;
using Moq;
using NLog.Extensions.Logging;
using NUnit.Framework;

namespace Tailer.Tests
{
    [TestFixture]
    public class ProgramTests
    {
        [Test]
        public void Should_Have_Path_Parameter_With_Default_Of_tmp_access_log()
        {
            IConfiguration program = new Program();
            Assert.AreEqual("/tmp/access.log", program.Path);
        }
        [Test]
        public void Should_Have_SkipContent_Parameter_With_Default_Of_False()
        {
            IConfiguration program = new Program();
            Assert.AreEqual(false, program.ParseExisting);
        }

        [Test]
        public void Should_Have_Reporting_Interval_Of_Ten_Seconds()
        {
            IConfiguration program = new Program();
            Assert.AreEqual(10, program.Interval);
        }

        [Test]
        public void Should_Have_Performance_Window_Of_Two_Minutes()
        {
            IConfiguration program = new Program();
            Assert.AreEqual(120, program.Window);
        }

        [Test]
        public void Should_Have_Threshold_Parameter_With_Default_Of_Ten_Seconds()
        {
            IConfiguration program = new Program();
            Assert.AreEqual(10, program.Threshold);
        }

        public void InitializeContainer(AutoMock mocker)
        {
            var builder = new ContainerBuilder();
            builder.Register(l => new LoggerFactory().AddNLog()).As<ILoggerFactory>();
            builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).SingleInstance();
            builder.RegisterInstance(mocker.Create<IStreamProcessor>()).As<IStreamProcessor>();
            Program.Container = builder.Build();
        }

        [Test]
        public void Program_Initialize_Should_Initialize_Loggers_and_StreamProcessors()
        {
            var p = new Program();
            p.Initialize();

            Assert.IsInstanceOf<ILogger<ProgramTests>>(Program.Container.Resolve<ILogger<ProgramTests>>());
            Assert.IsInstanceOf<W3CStats>(Program.Container.Resolve<IStatistician>());
            Assert.IsInstanceOf<TailProcessor>(Program.Container.Resolve<IStreamProcessor>());
        }


        [Test]
        public void Should_Process_The_File_Path()
        {
            using (var mock = AutoMock.GetLoose())
            {
                mock.Mock<IStreamProcessor>().Setup(p => p.Process(It.IsAny<FileStream>(), It.IsAny<long>()));
                InitializeContainer(mock);

                // Make a file for test purposes
                var path = Path.GetTempFileName();
                var program = new Program
                {
                    Path = path
                };

                program.OnExecute();

                mock.Mock<IStreamProcessor>().VerifyAll();
            }
        }
    }
}
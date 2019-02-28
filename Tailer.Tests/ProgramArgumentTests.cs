using NUnit.Framework;

namespace Tailer.Tests
{
    [TestFixture]
    public class ProgramArguments
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
            Assert.AreEqual(false, program.SkipContent);
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
    }
}
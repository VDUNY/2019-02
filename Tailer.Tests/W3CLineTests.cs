using System;
using NUnit.Framework;

namespace Tailer.Tests
{
    [TestFixture]
    public class W3CLineTests
    {
        [Test]
        public void Should_Parse_Log_Line_Example_1()
        {
            var line = new W3CLine("127.0.0.1 - james [09/May/2018:16:00:39 +0000] \"GET /report HTTP/1.0\" 200 123");

            Assert.AreEqual("127.0.0.1", line.RemoteClient);
            Assert.AreEqual("james", line.AuthenticatedUser);
            Assert.AreEqual(DateTimeOffset.Parse("2018-05-09T16:00:39+00:00"), line.Time);
            Assert.AreEqual("GET", line.Request.Verb);
            Assert.AreEqual("report", line.Request.Path);
            Assert.AreEqual(200, line.Status);
            Assert.AreEqual(123, line.Bytes);
        }

        [Test]
        public void Should_Parse_Log_Line_Example_2()
        {
            var line = new W3CLine("127.0.0.1 - jill [09/May/2018:16:00:41 +0000] \"GET /api/user HTTP/1.0\" 200 234");

            Assert.AreEqual("127.0.0.1", line.RemoteClient);
            Assert.AreEqual("jill", line.AuthenticatedUser);
            Assert.AreEqual(DateTimeOffset.Parse("2018-05-09T16:00:41+00:00"), line.Time);
            Assert.AreEqual("GET", line.Request.Verb);
            Assert.AreEqual("api", line.Request.Path);
            Assert.AreEqual(200, line.Status);
            Assert.AreEqual(234, line.Bytes);
        }

        [Test]
        public void Should_Parse_Full_Log_Line_Example_1()
        {
            var line = new W3CLine("192.32.52.119 - - [05/Feb/2019:22:31:11 -0500] \"PUT /list HTTP/1.0\" 303 5039 \"http://english.com/home/\" \"Mozilla/5.0 (Windows; U; Windows CE) AppleWebKit/535.13.3 (KHTML, like Gecko) Version/5.0 Safari/535.13.3\"");

            Assert.AreEqual("192.32.52.119", line.RemoteClient);
            Assert.AreEqual("-", line.AuthenticatedUser);
            Assert.AreEqual(DateTimeOffset.Parse("2019-02-05T22:31:11-05:00"), line.Time);
            Assert.AreEqual("PUT", line.Request.Verb);
            Assert.AreEqual("list", line.Request.Path);
            Assert.AreEqual(303, line.Status);
            Assert.AreEqual(5039, line.Bytes);
        }

        [Test]
        public void Should_Include_The_Full_Original_Request()
        {
            var line = new W3CLine("127.0.0.1 - frank [09/May/2018:16:00:42 +0000] \"POST /api/user HTTP/1.0\" 200 34");

            Assert.AreEqual("POST /api/user HTTP/1.0", line.Request.OriginalRequest);
        }
    }
}
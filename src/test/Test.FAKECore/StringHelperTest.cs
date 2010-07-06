using System.Collections.Generic;
using System.Linq;
using Fake;
using NUnit.Framework;
using Test.Git;

namespace Test.FAKECore
{
    [TestFixture]
    public class StringHelperTest
    {
        [Test]
        public void CanReadTestFile()
        {
            var lines = StringHelper.ReadFile(@"TestData\AllObjects.txt");
            lines.Count().ShouldEqual(3578);
        }

        [Test]
        public void CanSeparateEmptyLines()
        {
            var lines = new List<string>();
            StringHelper.separated("test", lines).ShouldBeEmpty();
        }

        [Test]
        public void CanSeparateOneLine()
        {
            var lines = new List<string> {"first"};
            StringHelper.separated("test", lines).ShouldEqual("first");
        }


        [Test]
        public void CanSeparateThreeLines()
        {
            var lines = new List<string> {"first", "second", "third"};
            StringHelper.separated("-", lines).ShouldEqual("first-second-third");
        }

        [Test]
        public void CanSeparateThreeLinesWithLineEnds()
        {
            var lines = new List<string> {"first", "second", "third"};
            StringHelper.toLines(lines).ShouldEqual("first\r\nsecond\r\nthird");
        }

        [Test]
        public void CanSeparateTwoLine()
        {
            var lines = new List<string> {"first", "second"};
            StringHelper.separated(" ", lines).ShouldEqual("first second");
        }
    }
}
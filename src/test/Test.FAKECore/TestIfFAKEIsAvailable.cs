using Fake;
using NUnit.Framework;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore
{
    [TestFixture]
    public class TestIfFakeIsAvailable : BaseTest
    {
        /// <summary>
        ///   Tests the fake path.
        /// </summary>
        [Test]
        public void TestFakePath()
        {
            Assert.IsNotNullOrEmpty(TraceHelper.fakePath);
        }

        /// <summary>
        ///   Tests the fake version.
        /// </summary>
        [Test]
        public void TestFakeVersion()
        {
            Assert.IsNotNullOrEmpty(TraceHelper.fakeVersionStr);
        }
    }
}
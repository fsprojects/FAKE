using Fake;
using NUnit.Framework;

namespace Test.FAKECore
{
    [TestFixture]
    public class TestIfFAKEIsAvailable
    {
        /// <summary>
        /// Tests the fake path.
        /// </summary>
        [Test]
        public void TestFakePath()
        {
            Assert.IsNotNullOrEmpty(TraceHelper.fakePath);
        }

        /// <summary>
        /// Tests the fake version.
        /// </summary>
        [Test]
        public void TestFakeVersion()
        {
            Assert.IsNotNullOrEmpty(TraceHelper.fakeVersionStr);
        }
    }
}
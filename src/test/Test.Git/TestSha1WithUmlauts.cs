using Fake.Git;
using NUnit.Framework;

namespace Test.Git
{
    [TestFixture]
    public class TestSha1WithUmlauts
    {
        [Test]
        public void CanCalcSha1OfSmallUe()
        {
            SHA1.calcGitSHA1("ü")
                .ShouldEqual("0f0f3e3b1ff2bc6722afc3e3812e6b782683896f");
        }


        [Test]
        public void CanCalcSha1OfSmallUeInTwoChars()
        {
            SHA1.calcGitSHA1("ue")
                .ShouldEqual("08e195cdf64898fc8c62dcd024f863dad66d15a2");
        }
    }
}
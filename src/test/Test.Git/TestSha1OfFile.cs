using System.IO;
using System.Text;
using Fake.Git;
using NUnit.Framework;

namespace Test.Git
{
    [TestFixture]
    public class TestSha1OfFile
    {
        [Test]
        public void CanCalcSha1OfFoo()
        {
            SHA1.calcGitSHA1(File.ReadAllText("./testfiles/foo.txt"))
                .ShouldEqual("74347b6a967594ae0c820171a2bc9542955a4c7f");
        }

        [Test]
        public void CanCalcSha1OfSmallOeEncoded()
        {
            SHA1.calcGitSHA1(File.ReadAllText("./testfiles/oe.txt", Encoding.Default))
                .ShouldEqual("16e45d390712388410556b522c74a11637716844");
        }

        [Test]
        public void CanCalcSha1OfSmallUe()
        {
            SHA1.calcGitSHA1(File.ReadAllText("./testfiles/ue.txt", Encoding.Default))
                .ShouldEqual("0f0f3e3b1ff2bc6722afc3e3812e6b782683896f");
        }
    }
}
using Fake.Git;
using NUnit.Framework;

namespace Test.Git
{
    [TestFixture]
    public class TestSha1
    {
        [Test]
        public void CanCalcSha1OfEmptyString()
        {
            SHA1.calcGitSHA1("")
                .ShouldEqual("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391");
        }

        [Test]
        public void CanCalcSha1OfFoobar()
        {
            SHA1.calcGitSHA1("foobar")
                .ShouldEqual("f6ea0495187600e7b2288c8ac19c5886383a4632");
        }

        [Test]
        public void CanCalcSha1OfFoobarAndCrLn()
        {
            SHA1.calcGitSHA1("foobar\r\n")
                .ShouldEqual("323fae03f4606ea9991df8befbb2fca795e648fa");
        }

        [Test]
        public void CanCalcSha1OfFoobarAndFoo()
        {
            SHA1.calcGitSHA1("foobar\r\nfoo\r\n")
                .ShouldEqual("cbe739ff1e2005b0850200da710fedc248549063");
        }

        [Test]
        public void CanCalcSha1OfFoobarAndFoo2()
        {
            SHA1.calcGitSHA1("foobar\r\nfoo\r\n new FO()")
                .ShouldEqual("74347b6a967594ae0c820171a2bc9542955a4c7f");
        }

        [Test]
        public void CanCalcSha1OfFoobarAndFooWithMacLineEnds()
        {
            SHA1.calcGitSHA1("foobar\rfoo\r new FO()")
                .ShouldEqual("b06e4cfe20adfeeb69cac3e5d941f02deda9fca3");
        }

        [Test]
        public void CanCalcSha1OfFoobarAndLn()
        {
            SHA1.calcGitSHA1("foobar\n")
                .ShouldEqual("323fae03f4606ea9991df8befbb2fca795e648fa");
        }
    }
}
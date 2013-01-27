using System.IO;
using System.Text;
using Fake.Git;
using Machine.Specifications;

namespace Test.Git
{
    public class when_calculating_sha1
    {
        It should_calculate_the_sha1_of_empty_string =
            () => SHA1.calcGitSHA1("")
                      .ShouldEqual("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391");

        It should_calculate_the_sha1_of_foobar =
            () => SHA1.calcGitSHA1("foobar")
                      .ShouldEqual("f6ea0495187600e7b2288c8ac19c5886383a4632");

        It should_calculate_the_sha1_of_foobar_and_foo =
            () => SHA1.calcGitSHA1("foobar\r\nfoo\r\n")
                      .ShouldEqual("cbe739ff1e2005b0850200da710fedc248549063");

        It should_calculate_the_sha1_of_foobar_and_foo2 =
            () => SHA1.calcGitSHA1("foobar\r\nfoo\r\n new FO()")
                      .ShouldEqual("74347b6a967594ae0c820171a2bc9542955a4c7f");

        It should_calculate_the_sha1_of_foobar_and_foo_with_mac_lineends =
            () => SHA1.calcGitSHA1("foobar\rfoo\r new FO()")
                      .ShouldEqual("b06e4cfe20adfeeb69cac3e5d941f02deda9fca3");

        It should_calculate_the_sha1_of_foobar_and_lineend =
            () => SHA1.calcGitSHA1("foobar\r\n")
                      .ShouldEqual("323fae03f4606ea9991df8befbb2fca795e648fa");

        It should_calculate_the_sha1_of_foobar_and_ln =
            () => SHA1.calcGitSHA1("foobar\n")
                      .ShouldEqual("323fae03f4606ea9991df8befbb2fca795e648fa");

        It should_calculate_the_sha1_of_small_umlaut_as_two_chars =
            () => SHA1.calcGitSHA1("ue")
                      .ShouldEqual("08e195cdf64898fc8c62dcd024f863dad66d15a2");
    }

    public class when_calculating_sha1_of_a_file
    {
        It should_calculate_the_sha1_of_foo =
            () => SHA1.calcGitSHA1(File.ReadAllText("./testfiles/foo.txt"))
                      .ShouldEqual("74347b6a967594ae0c820171a2bc9542955a4c7f");
    }
}
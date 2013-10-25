using System.Collections.Generic;
using System.Linq;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_parsing_semver_strings_and_printing_the_result
    {
        It should_keep_0_1_2 =
            () => SemVerHelper.parse("0.1.2").ToString().ShouldEqual("0.1.2");

        It should_keep_1_0_2 =
            () => SemVerHelper.parse("1.0.2").ToString().ShouldEqual("1.0.2");

        It should_expand_1_0 =
            () => SemVerHelper.parse("1.0").ToString().ShouldEqual("1.0.0");

        It should_parse_alpha_versions =
            () => SemVerHelper.parse("1.0.0-alpha.1").ToString().ShouldEqual("1.0.0-alpha.1");

        It should_parse_beta_versions =
            () => SemVerHelper.parse("1.0.0-beta.2").ToString().ShouldEqual("1.0.0-beta.2");

        It should_parse_alpha_beta_versions =
            () => SemVerHelper.parse("1.0.0-alpha.beta").ToString().ShouldEqual("1.0.0-alpha.beta");

        It should_parse_rc_versions =
            () => SemVerHelper.parse("1.0.0-rc.1").ToString().ShouldEqual("1.0.0-rc.1");
    }

    public class when_parsing_semver_strings
    {
        static SemVerHelper.SemVerInfo semVer;
        Because of = () => semVer = SemVerHelper.parse("1.2.3-alpha.beta");

        It should_parse_major = () => semVer.Major.ShouldEqual(1);
        It should_parse_minor = () => semVer.Minor.ShouldEqual(2);
        It should_parse_patch = () => semVer.Patch.ShouldEqual(3);
        It should_parse_prerelease = () => semVer.PreRelease.ShouldEqual("alpha");
        It should_parse_build = () => semVer.Build.ShouldEqual("beta");
    }
}
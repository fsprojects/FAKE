using System.Collections.Generic;
using System.Linq;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Core;

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

        It should_parse_prerelease_versions_without_build =
            () => SemVerHelper.parse("1.2.3-foo").ToString().ShouldEqual("1.2.3-foo");
    }

    public class when_validating_semver_strings
    {
        It should_validate_0_1_2 =
            () => SemVerHelper.isValidSemVer("0.1.2").ShouldEqual(true);

        It should_validate_alpha_beta_versions =
            () => SemVerHelper.isValidSemVer("1.0.0-alpha.beta").ShouldEqual(true);

        It should_validate_prerelease_versions_without_build =
            () => SemVerHelper.isValidSemVer("1.2.3-foo").ShouldEqual(true);

        It should_reject_leading_zeros = 
            () => SemVerHelper.isValidSemVer("01.02.03").ShouldEqual(false);

        It should_require_3_numbers = 
            () => SemVerHelper.isValidSemVer("1.2").ShouldEqual(false);

        It should_reject_leading_v = 
            () => SemVerHelper.isValidSemVer("v1.2.0").ShouldEqual(false);


    }


    public class when_parsing_semver_strings
    {
        static SemVerHelper.SemVerInfo semVer;
        Because of = () => semVer = SemVerHelper.parse("1.2.3-alpha+beta");

        It should_parse_major = () => semVer.Major.ShouldEqual(1);
        It should_parse_minor = () => semVer.Minor.ShouldEqual(2);
        It should_parse_patch = () => semVer.Patch.ShouldEqual(3);
        It should_parse_prerelease = () => semVer.PreRelease.ShouldEqual(
            FSharpOption<SemVerHelper.PreRelease>.Some(new SemVerHelper.PreRelease("alpha", "alpha", FSharpOption<int>.None, new[] { SemVerHelper.Ident.NewAlphaNumeric("alpha") }.ToFSharpList())));
        It should_parse_build = () => semVer.Build.ShouldEqual("beta");
    }

    public class when_comparing_semvers
    {
        It should_detect_equal_versions =
            () => SemVerHelper.parse("1.2.3")
                .ShouldEqual(SemVerHelper.parse("1.2.3"));

        It should_compare_rc_versions =
            () => SemVerHelper.parse("1.0.0-rc.3")
                .ShouldBeGreaterThan(SemVerHelper.parse("1.0.0-rc.1"));

        It should_compare_alpha_versions =
            () => SemVerHelper.parse("1.0.0-alpha.3")
                .ShouldBeGreaterThan(SemVerHelper.parse("1.0.0-alpha.2"));

        It should_detect_equal_alpha_versions =
            () => SemVerHelper.parse("1.2.3-alpha.3")
                .ShouldEqual(SemVerHelper.parse("1.2.3-alpha.3"));

        It should_assume_empty_build_is_smaller_than_specified_build =
            () => SemVerHelper.parse("1.0.0-alpha")
                .ShouldBeLessThan(SemVerHelper.parse("1.0.0-alpha.1"));

        It should_assume_no_in_build_is_smaller_than_text_in_build =
            () => SemVerHelper.parse("1.0.0-alpha.1")
                .ShouldBeLessThan(SemVerHelper.parse("1.0.0-alpha.beta"));

        It should_assume_that_longer_prereleases_are_greater =
            () => SemVerHelper.parse("1.0.0-alpha.beta")
                .ShouldBeGreaterThan(SemVerHelper.parse("1.0.0-beta"));

        It should_assume_empty_build_no_in_beta_build_is_smaller_than_text_in_build =
            () => SemVerHelper.parse("1.0.0-beta")
                .ShouldBeLessThan(SemVerHelper.parse("1.0.0-beta.2"));

        It should_assume_smaller_build_no_are_smaller =
            () => SemVerHelper.parse("1.0.0-beta.2")
                .ShouldBeLessThan(SemVerHelper.parse("1.0.0-beta.11"));

        It should_assume_beta_is_smaller_than_rc =
            () => SemVerHelper.parse("1.0.0-beta.11")
                .ShouldBeLessThan(SemVerHelper.parse("1.0.0-rc.1"));

        It should_assume_rc_is_smaller_than_release =
            () => SemVerHelper.parse("1.0.0-rc.1")
                .ShouldBeLessThan(SemVerHelper.parse("1.0.0"));

        It should_assume_release_is_greater_than_alpha =
            () => SemVerHelper.parse("2.3.4")
                .ShouldBeGreaterThan(SemVerHelper.parse("2.3.4-alpha"));

        It should_assume_beta_2_is_smaller_than_rc_1 =
            () => SemVerHelper.parse("1.5.0-rc.1")
                .ShouldBeGreaterThan(SemVerHelper.parse("1.5.0-beta.2"));

        It should_assume_alpha2_is_greater_than_alpha =
            () => SemVerHelper.parse("2.3.4-alpha2")
                .ShouldBeGreaterThan(SemVerHelper.parse("2.3.4-alpha"));

        It should_assume_alpha003_is_less_than_alpha2_because_lexicalsort =
            () => SemVerHelper.parse("2.3.4-alpha003")
                .ShouldBeLessThan(SemVerHelper.parse("2.3.4-alpha2"));

        It should_assume_rc_is_greater_than_beta2 =
            () => SemVerHelper.parse("2.3.4-rc")
                .ShouldBeGreaterThan(SemVerHelper.parse("2.3.4-beta2"));
    }
}
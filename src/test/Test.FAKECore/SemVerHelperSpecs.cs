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
    }
}
using System.Collections.Generic;
using System.Linq;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_encapsuting_strings
    {
        It should_encapsulate_without_special_chars =
            () => TeamCityHelper.EncapsulateSpecialChars("Client FR, Total 47, Failures 1, NotRun 0, Inconclusive 0, Skipped 0")
                      .ShouldEqual("Client FR, Total 47, Failures 1, NotRun 0, Inconclusive 0, Skipped 0");
    }

    public class when_creating_buildstatus
    {
        It should_encapsulate_wspecial_chars =
            () => TeamCityHelper.buildStatus("FAILURE", "Client FR, Total 47, Failures 1, NotRun 0, Inconclusive 0, Skipped 0")
                      .ShouldEqual("##teamcity[buildStatus 'FAILURE' text='Client FR, Total 47, Failures 1, NotRun 0, Inconclusive 0, Skipped 0']");
    }
}
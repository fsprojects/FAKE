using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.MSBuild
{
    public class when_using_a_logger
    {
        It should_find_the_error_logger =
            () => MSBuildHelper.ErrorLoggerName.ShouldEqual("Fake.MsBuildLogger+ErrorLogger");

        It should_find_the_teamcity_logger =
            () => MSBuildHelper.TeamCityLoggerName.ShouldEqual("Fake.MsBuildLogger+TeamCityLogger");
    }
}
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.MSBuild
{
    public class when_using_a_logger
    {
        It should_build_the_logger_from_filepath_with_hash = () =>
            MSBuildHelper.buildErrorLoggerParam("file:///C:/Test#/asd")
                .ShouldEqual(
                    "/logger:Fake.MsBuildLogger+TeamCityLogger,\"C:\\Test#\\asd\" /logger:Fake.MsBuildLogger+ErrorLogger,\"C:\\Test#\\asd\"");

        It should_build_the_logger_from_simple_folder = () =>
            MSBuildHelper.buildErrorLoggerParam("file:///C:/Test")
                .ShouldEqual(
                    "/logger:Fake.MsBuildLogger+TeamCityLogger,\"C:\\Test\" /logger:Fake.MsBuildLogger+ErrorLogger,\"C:\\Test\"");

        It should_find_the_error_logger =
            () => MSBuildHelper.ErrorLoggerName.ShouldEqual("Fake.MsBuildLogger+ErrorLogger");

        It should_find_the_teamcity_logger =
            () => MSBuildHelper.TeamCityLoggerName.ShouldEqual("Fake.MsBuildLogger+TeamCityLogger");
    }
}
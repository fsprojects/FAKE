using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.MSBuild
{
    public class when_using_a_logger
    {
        It should_build_the_logger_from_filepath_with_hash = () =>
            MSBuildHelper.buildErrorLoggerParam("file:///C:/Test#/asd")
                .ShouldEqual(
                    string.Format(
                        "/logger:Fake.MsBuildLogger+TeamCityLogger,\"C:{0}Test#{0}asd\" /logger:Fake.MsBuildLogger+ErrorLogger,\"C:{0}Test#{0}asd\"",
                        Path.DirectorySeparatorChar));

        It should_build_the_logger_from_simple_folder = () =>
            MSBuildHelper.buildErrorLoggerParam("file:///C:/Test")
                .ShouldEqual(
                    string.Format(
                        "/logger:Fake.MsBuildLogger+TeamCityLogger,\"C:{0}Test\" /logger:Fake.MsBuildLogger+ErrorLogger,\"C:{0}Test\"",
                        Path.DirectorySeparatorChar));

        It should_find_the_error_logger =
            () => MSBuildHelper.ErrorLoggerName.ShouldEqual("Fake.MsBuildLogger+ErrorLogger");

        It should_find_the_teamcity_logger =
            () => MSBuildHelper.TeamCityLoggerName.ShouldEqual("Fake.MsBuildLogger+TeamCityLogger");
    }
}
using System;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class OpenCoverHelperSpecs
    {
        It args_should_include_mergebyhash = () => BuildOpenCoverArgs(true).ShouldContain(" -mergebyhash");
        It args_should_not_include_mergebyhash = () => BuildOpenCoverArgs(false).ShouldNotContain(" -mergebyhash");
        It args_should_include_target_args = () => BuildOpenCoverArgs(false).ShouldContain(" -targetargs:\"Tests\\test.dll\"");
        It args_should_include_target = () => 
            BuildOpenCoverArgs(false).ShouldContain(string.Format("-target:\"{0}\" ", FileSystemHelper.FullName("TestRunnerPath.exe")));
        It args_should_include_RegisterUser = () => BuildOpenCoverArgs(false).ShouldContain(" -register:user");
        It args_should_include_filter = () => BuildOpenCoverArgs(false).ShouldContain(" -filter:\"[*Tests]*\"");
        It args_should_include_optionalArguments = () => BuildOpenCoverArgs(false, "-enableperformancecounters").ShouldContain("-enableperformancecounters");

        private static string BuildOpenCoverArgs(bool mergebyhash, string optionalArguments = "")
        {
            return OpenCoverHelper.buildOpenCoverArgs(
                new OpenCoverHelper.OpenCoverParams("Exepath.exe", "TestRunnerPath.exe", "output",
                    OpenCoverHelper.RegisterType.RegisterUser, "[*Tests]*", new TimeSpan(10), "WorkingDir", mergebyhash, optionalArguments), 
                    "Tests\\test.dll");
        }
    }
}
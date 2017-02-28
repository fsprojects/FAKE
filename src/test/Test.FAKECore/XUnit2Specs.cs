using System;
using Fake;
using Fake.Testing;
using Machine.Specifications;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace Test.FAKECore.XUnit2Specs
{
    [Subject(typeof(XUnit2), "runner argument construction")]
    internal abstract class BuildArgumentsSpecsBase
    {
        Establish context = () =>
        {
            Parameters = XUnit2.XUnit2Defaults;
            Assemblies = new[] { "test.dll", "other.dll" };
        };

        Because of = () =>
        {
            Arguments = XUnit2.buildXUnit2Args(Assemblies, Parameters);
            Console.WriteLine(Arguments);
        };

        protected static XUnit2.XUnit2Params Parameters;
        protected static string[] Assemblies;
        protected static string Arguments;
    }

    internal class When_using_the_default_parameters
        : BuildArgumentsSpecsBase
    {
        It should_not_include_any_traits = () =>
        {
            Arguments.ShouldNotContain(" -trait");
            Arguments.ShouldNotContain(" -notrait");
        };

        It should_include_the_requested_assembly_in_quotes = () =>
        {
            Arguments.ShouldContain(@"""test.dll""");
            Arguments.ShouldContain(@"""other.dll""");
        };

        It should_not_include_any_report_options = () =>
        {
            Arguments.ShouldNotContain(" -xml");
            Arguments.ShouldNotContain(" -xmlv1");
            Arguments.ShouldNotContain(" -nunit");
            Arguments.ShouldNotContain(" -html");
        };

        It should_request_no_parallelism = () =>
            Arguments.ShouldContain(" -parallel none");

        It should_request_default_max_threads = () =>
            Arguments.ShouldNotContain(" -maxthreads");

        It should_not_request_no_shadow_copy = () =>
            Arguments.ShouldNotContain(" -noshadow");

        It should_not_request_silence = () =>
            Arguments.ShouldNotContain(" -quiet");

        It should_not_request_wait = () =>
            Arguments.ShouldNotContain(" -wait");

        It should_not_force_TeamCity_output = () =>
            Arguments.ShouldNotContain(" -teamcity");

        It should_not_force_AppVeyor_output = () =>
            Arguments.ShouldNotContain(" -appveyor");

        It should_not_force_NoAppDomain = () =>
            Arguments.ShouldNotContain(" -noappdomain");
    }

    internal class When_using_parameters_which_include_traits
        : BuildArgumentsSpecsBase
    {
        Establish context = () =>
        {
            Parameters = new XUnit2.XUnit2Params(
                XUnit2.XUnit2Defaults.ToolPath,
                XUnit2.XUnit2Defaults.NoAppDomain,
                XUnit2.XUnit2Defaults.Parallel,
                XUnit2.XUnit2Defaults.MaxThreads,
                XUnit2.XUnit2Defaults.HtmlOutputPath,
                XUnit2.XUnit2Defaults.XmlOutputPath,
                XUnit2.XUnit2Defaults.XmlV1OutputPath,
                XUnit2.XUnit2Defaults.NUnitXmlOutputPath,
                XUnit2.XUnit2Defaults.WorkingDir,
                XUnit2.XUnit2Defaults.ShadowCopy,
                XUnit2.XUnit2Defaults.Silent,
                XUnit2.XUnit2Defaults.TimeOut,
                XUnit2.XUnit2Defaults.ErrorLevel,
                Util.Traits(Util.Trait("name", "value1"), Util.Trait("name2", "value2")),
                Util.Traits(Util.Trait("name", "value3")),
                XUnit2.XUnit2Defaults.ForceTeamCity,
                XUnit2.XUnit2Defaults.ForceAppVeyor,
                XUnit2.XUnit2Defaults.Wait,
                XUnit2.XUnit2Defaults.Namespace,
                XUnit2.XUnit2Defaults.Class,
                XUnit2.XUnit2Defaults.Method);
        };

        It should_include_the_expected_include_trait_arguments = () =>
        {
            Arguments.ShouldContain(@" -trait ""name=value1""");
            Arguments.ShouldContain(@" -trait ""name2=value2""");
        };

        It should_include_the_expected_exclude_trait_arguments = () =>
            Arguments.ShouldContain(@" -notrait ""name=value3""");
    }

    internal class When_using_parameters_which_include_reports
        : BuildArgumentsSpecsBase
    {
        Establish context = () =>
        {
            Parameters = new XUnit2.XUnit2Params(
                XUnit2.XUnit2Defaults.ToolPath,
                XUnit2.XUnit2Defaults.NoAppDomain,
                XUnit2.XUnit2Defaults.Parallel,
                XUnit2.XUnit2Defaults.MaxThreads,
                FSharpOption<string>.Some("html.html"),
                FSharpOption<string>.Some("xml.xml"),
                FSharpOption<string>.Some("xmlv1.xml"),
                FSharpOption<string>.Some("nunit.xml"),
                XUnit2.XUnit2Defaults.WorkingDir,
                XUnit2.XUnit2Defaults.ShadowCopy,
                XUnit2.XUnit2Defaults.Silent,
                XUnit2.XUnit2Defaults.TimeOut,
                XUnit2.XUnit2Defaults.ErrorLevel,
                XUnit2.XUnit2Defaults.IncludeTraits,
                XUnit2.XUnit2Defaults.ExcludeTraits,
                XUnit2.XUnit2Defaults.ForceTeamCity,
                XUnit2.XUnit2Defaults.ForceAppVeyor,
                XUnit2.XUnit2Defaults.Wait,
                XUnit2.XUnit2Defaults.Namespace,
                XUnit2.XUnit2Defaults.Class,
                XUnit2.XUnit2Defaults.Method);
        };

        It should_include_the_expected_HTML_reporting_argument = () =>
            Arguments.ShouldContain(@" -html ""html.html""");

        It should_include_the_expected_XML_reporting_argument = () =>
            Arguments.ShouldContain(@" -xml ""xml.xml""");

        It should_include_the_expected_XML_v1_reporting_argument = () =>
            Arguments.ShouldContain(@" -xmlv1 ""xmlv1.xml""");

        It should_include_the_expected_NUnit_XML_reporting_argument = () =>
            Arguments.ShouldContain(@" -nunit ""nunit.xml""");
    }

    internal class When_using_parameters_which_request_total_parallel_execution
        : BuildArgumentsSpecsBase
    {
        Establish context = () =>
        {
            Parameters = new XUnit2.XUnit2Params(
                XUnit2.XUnit2Defaults.ToolPath,
                XUnit2.XUnit2Defaults.NoAppDomain,
                XUnit2.ParallelMode.All,
                XUnit2.CollectionConcurrencyMode.Unlimited,
                XUnit2.XUnit2Defaults.HtmlOutputPath,
                XUnit2.XUnit2Defaults.XmlOutputPath,
                XUnit2.XUnit2Defaults.XmlV1OutputPath,
                XUnit2.XUnit2Defaults.NUnitXmlOutputPath,
                XUnit2.XUnit2Defaults.WorkingDir,
                XUnit2.XUnit2Defaults.ShadowCopy,
                XUnit2.XUnit2Defaults.Silent,
                XUnit2.XUnit2Defaults.TimeOut,
                XUnit2.XUnit2Defaults.ErrorLevel,
                XUnit2.XUnit2Defaults.IncludeTraits,
                XUnit2.XUnit2Defaults.ExcludeTraits,
                XUnit2.XUnit2Defaults.ForceTeamCity,
                XUnit2.XUnit2Defaults.ForceAppVeyor,
                XUnit2.XUnit2Defaults.Wait,
                XUnit2.XUnit2Defaults.Namespace,
                XUnit2.XUnit2Defaults.Class,
                XUnit2.XUnit2Defaults.Method);
        };

        It should_include_the_expected_parallel_argument = () =>
            Arguments.ShouldContain(@" -parallel all");

        It should_include_the_expected_maxthreads_argument = () =>
            Arguments.ShouldContain(@" -maxthreads 0");
    }

    internal class When_using_parameters_which_request_assembly_only_parallel_execution
        : BuildArgumentsSpecsBase
    {
        Establish context = () =>
        {
            Parameters = new XUnit2.XUnit2Params(
                XUnit2.XUnit2Defaults.ToolPath,
                XUnit2.XUnit2Defaults.NoAppDomain,
                XUnit2.ParallelMode.Assemblies,
                XUnit2.CollectionConcurrencyMode.Default,
                XUnit2.XUnit2Defaults.HtmlOutputPath,
                XUnit2.XUnit2Defaults.XmlOutputPath,
                XUnit2.XUnit2Defaults.XmlV1OutputPath,
                XUnit2.XUnit2Defaults.NUnitXmlOutputPath,
                XUnit2.XUnit2Defaults.WorkingDir,
                XUnit2.XUnit2Defaults.ShadowCopy,
                XUnit2.XUnit2Defaults.Silent,
                XUnit2.XUnit2Defaults.TimeOut,
                XUnit2.XUnit2Defaults.ErrorLevel,
                XUnit2.XUnit2Defaults.IncludeTraits,
                XUnit2.XUnit2Defaults.ExcludeTraits,
                XUnit2.XUnit2Defaults.ForceTeamCity,
                XUnit2.XUnit2Defaults.ForceAppVeyor,
                XUnit2.XUnit2Defaults.Wait,
                XUnit2.XUnit2Defaults.Namespace,
                XUnit2.XUnit2Defaults.Class,
                XUnit2.XUnit2Defaults.Method);
        };

        It should_include_the_expected_parallel_argument = () =>
            Arguments.ShouldContain(@" -parallel assemblies");

        It should_include_the_expected_maxthreads_argument = () =>
            Arguments.ShouldNotContain(@" -maxthreads");
    }

    internal class When_using_parameters_which_request_collection_only_parallel_execution
        : BuildArgumentsSpecsBase
    {
        Establish context = () =>
        {
            Parameters = new XUnit2.XUnit2Params(
                XUnit2.XUnit2Defaults.ToolPath,
                XUnit2.XUnit2Defaults.NoAppDomain,
                XUnit2.ParallelMode.Collections,
                XUnit2.CollectionConcurrencyMode.NewMaxThreads(42),
                XUnit2.XUnit2Defaults.HtmlOutputPath,
                XUnit2.XUnit2Defaults.XmlOutputPath,
                XUnit2.XUnit2Defaults.XmlV1OutputPath,
                XUnit2.XUnit2Defaults.NUnitXmlOutputPath,
                XUnit2.XUnit2Defaults.WorkingDir,
                XUnit2.XUnit2Defaults.ShadowCopy,
                XUnit2.XUnit2Defaults.Silent,
                XUnit2.XUnit2Defaults.TimeOut,
                XUnit2.XUnit2Defaults.ErrorLevel,
                XUnit2.XUnit2Defaults.IncludeTraits,
                XUnit2.XUnit2Defaults.ExcludeTraits,
                XUnit2.XUnit2Defaults.ForceTeamCity,
                XUnit2.XUnit2Defaults.ForceAppVeyor,
                XUnit2.XUnit2Defaults.Wait,
                XUnit2.XUnit2Defaults.Namespace,
                XUnit2.XUnit2Defaults.Class,
                XUnit2.XUnit2Defaults.Method);
            Assemblies = new[] { "test.dll", "other.dll" };
        };

        It should_include_the_expected_parallel_argument = () =>
            Arguments.ShouldContain(@" -parallel collections");

        It should_include_the_expected_maxthreads_argument = () =>
            Arguments.ShouldContain(@" -maxthreads 42");
    }

    internal class When_using_parameters_which_request_non_default_flags
        : BuildArgumentsSpecsBase
    {
        Establish context = () =>
        {
            Parameters = new XUnit2.XUnit2Params(
                XUnit2.XUnit2Defaults.ToolPath,
                !XUnit2.XUnit2Defaults.NoAppDomain,
                XUnit2.XUnit2Defaults.Parallel,
                XUnit2.XUnit2Defaults.MaxThreads,
                XUnit2.XUnit2Defaults.HtmlOutputPath,
                XUnit2.XUnit2Defaults.XmlOutputPath,
                XUnit2.XUnit2Defaults.XmlV1OutputPath,
                XUnit2.XUnit2Defaults.NUnitXmlOutputPath,
                XUnit2.XUnit2Defaults.WorkingDir,
                !XUnit2.XUnit2Defaults.ShadowCopy,
                !XUnit2.XUnit2Defaults.Silent,
                XUnit2.XUnit2Defaults.TimeOut,
                XUnit2.XUnit2Defaults.ErrorLevel,
                XUnit2.XUnit2Defaults.IncludeTraits,
                XUnit2.XUnit2Defaults.ExcludeTraits,
                !XUnit2.XUnit2Defaults.ForceTeamCity,
                !XUnit2.XUnit2Defaults.ForceAppVeyor,
                !XUnit2.XUnit2Defaults.Wait,
                XUnit2.XUnit2Defaults.Namespace,
                XUnit2.XUnit2Defaults.Class,
                XUnit2.XUnit2Defaults.Method);
        };

        It should_request_no_shadow_copy = () =>
            Arguments.ShouldContain(" -noshadow");

        It should_request_silence = () =>
            Arguments.ShouldContain(" -quiet");

        It should_request_wait = () =>
            Arguments.ShouldContain(" -wait");

        It should_force_TeamCity_output = () =>
            Arguments.ShouldContain(" -teamcity");

        It should_force_AppVeyor_output = () =>
            Arguments.ShouldContain(" -appveyor");

        It should_force_NoAppDomain = () =>
            Arguments.ShouldContain(" -noappdomain");
    }

    [Subject(typeof(XUnit2), "result handling")]
    internal abstract class XUnitResultHandlingSpecsBase
    {
        Because of = () =>
            Exception = Catch.Exception(() => XUnit2.ResultHandling.failBuildIfXUnitReportedError(ErrorLevel).Invoke(ErrorCode));

        protected static UnitTestCommon.TestRunnerErrorLevel ErrorLevel;
        protected static int ErrorCode;
        protected static Exception Exception;
    }

    internal class When_a_zero_exit_code_is_returned_and_the_task_is_configured_to_fail_on_an_error
        : XUnitResultHandlingSpecsBase
    {
        Establish context = () =>
        {
            ErrorLevel = UnitTestCommon.TestRunnerErrorLevel.Error;
            ErrorCode = 0;
        };

        It should_not_fail_the_build = () =>
            Exception.ShouldBeNull();
    }

    internal class When_a_zero_exit_code_is_returned_and_the_task_is_configured_to_fail_on_the_first_error
        : XUnitResultHandlingSpecsBase
    {
        Establish context = () =>
        {
            ErrorLevel = UnitTestCommon.TestRunnerErrorLevel.FailOnFirstError;
            ErrorCode = 0;
        };

        It should_not_fail_the_build = () =>
            Exception.ShouldBeNull();
    }

    internal class When_a_zero_exit_code_is_returned_and_the_task_is_configured_not_to_fail_the_build
        : XUnitResultHandlingSpecsBase
    {
        Establish context = () =>
        {
            ErrorLevel = UnitTestCommon.TestRunnerErrorLevel.DontFailBuild;
            ErrorCode = 0;
        };

        It should_not_fail_the_build = () =>
            Exception.ShouldBeNull();
    }

    internal class When_a_non_zero_exit_code_is_returned_and_the_task_is_configured_to_fail_on_an_error
        : XUnitResultHandlingSpecsBase
    {
        Establish context = () =>
        {
            ErrorLevel = UnitTestCommon.TestRunnerErrorLevel.Error;
            ErrorCode = 42;
        };

        It should_fail_the_build = () =>
            Exception.ShouldNotBeNull();
    }

    internal class When_a_non_zero_exit_code_is_returned_and_the_task_is_configured_to_fail_on_the_first_error
        : XUnitResultHandlingSpecsBase
    {
        Establish context = () =>
        {
            ErrorLevel = UnitTestCommon.TestRunnerErrorLevel.FailOnFirstError;
            ErrorCode = 42;
        };

        It should_fail_the_build = () =>
            Exception.ShouldNotBeNull();
    }

    internal class When_a_non_zero_exit_code_is_returned_and_the_task_is_configured_not_to_fail_the_build
        : XUnitResultHandlingSpecsBase
    {
        Establish context = () =>
        {
            ErrorLevel = UnitTestCommon.TestRunnerErrorLevel.DontFailBuild;
            ErrorCode = 42;
        };

        It should_not_fail_the_build = () =>
            Exception.ShouldBeNull();
    }

    internal static class Util
    {
        public static FSharpList<Tuple<string, string>> Traits(params Tuple<string, string>[] traits)
        {
            return ListModule.OfArray(traits);
        }

        public static Tuple<string, string> Trait(string name, string values)
        {
            return new Tuple<string, string>(name, values);
        }
    }
}

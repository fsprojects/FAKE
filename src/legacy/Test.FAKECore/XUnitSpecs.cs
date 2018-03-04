using System;
using System.Runtime.Remoting;
using Fake;
using Fake.Testing;
using Machine.Specifications;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace Test.FAKECore.Testing.XUnitSpecs
{
    [Subject(typeof(XUnit), "runner argument construction")]
    internal abstract class BuildArgumentsSpecsBase
    {
        Establish context = () =>
        {
            Assembly = "test.dll";
            Parameters = XUnit.XUnitDefaults;
        };

        Because of = () =>
        {
            Arguments = XUnit.buildXUnitArgs(Assembly, Parameters);
            Console.WriteLine(Arguments);
        };

        protected static XUnit.XUnitParams Parameters;
        protected static string Assembly;
        protected static string Arguments;
    }

    internal class When_using_the_default_parameters
        : BuildArgumentsSpecsBase
    {
        It should_not_include_any_traits = () =>
        {
            Arguments.ShouldNotContain(" /trait");
            Arguments.ShouldNotContain(" /-trait");
        };

        It should_include_the_requested_assembly_in_quotes = () =>
        {
            Arguments.ShouldContain(@"""test.dll""");
        };

        It should_not_include_any_report_options = () =>
        {
            Arguments.ShouldNotContain(" /xml");
            Arguments.ShouldNotContain(" /nunit");
            Arguments.ShouldNotContain(" /html");
        };

        It should_not_request_no_shadow_copy = () =>
            Arguments.ShouldNotContain(" /noshadow");

        It should_not_request_silence = () =>
            Arguments.ShouldNotContain(" /silent");

        It should_not_force_TeamCity_output = () =>
            Arguments.ShouldNotContain(" /teamcity");
    }

    internal class When_using_parameters_which_include_traits
        : BuildArgumentsSpecsBase
    {
        Establish context = () =>
        {
            Parameters = new XUnit.XUnitParams(
                XUnit.XUnitDefaults.ToolPath,
                XUnit.XUnitDefaults.HtmlOutputPath,
                XUnit.XUnitDefaults.NUnitXmlOutputPath,
                XUnit.XUnitDefaults.XmlOutputPath,
                XUnit.XUnitDefaults.WorkingDir,
                XUnit.XUnitDefaults.ShadowCopy,
                XUnit.XUnitDefaults.Silent,
                XUnit.XUnitDefaults.TimeOut,
                XUnit.XUnitDefaults.ErrorLevel,
                Util.Traits(Util.Trait("name", "value1"), Util.Trait("name2", "value2")),
                Util.Traits(Util.Trait("name", "value3")),
                XUnit.XUnitDefaults.ForceTeamCity);
        };

        It should_include_the_expected_include_trait_arguments = () =>
        {
            Arguments.ShouldContain(@" /trait ""name=value1""");
            Arguments.ShouldContain(@" /trait ""name2=value2""");
        };

        It should_include_the_expected_exclude_trait_arguments = () =>
            Arguments.ShouldContain(@" /-trait ""name=value3""");
    }

    internal class When_using_parameters_which_include_reports
        : BuildArgumentsSpecsBase
    {
        Establish context = () =>
        {
            Parameters = new XUnit.XUnitParams(
                XUnit.XUnitDefaults.ToolPath,
                FSharpOption<string>.Some("html.html"),
                FSharpOption<string>.Some("nunit.xml"),
                FSharpOption<string>.Some("xml.xml"),
                XUnit.XUnitDefaults.WorkingDir,
                XUnit.XUnitDefaults.ShadowCopy,
                XUnit.XUnitDefaults.Silent,
                XUnit.XUnitDefaults.TimeOut,
                XUnit.XUnitDefaults.ErrorLevel,
                XUnit.XUnitDefaults.IncludeTraits,
                XUnit.XUnitDefaults.ExcludeTraits,
                XUnit.XUnitDefaults.ForceTeamCity);
        };

        It should_include_the_expected_HTML_reporting_argument = () =>
            Arguments.ShouldContain(@" /html ""html.html""");

        It should_include_the_expected_XML_reporting_argument = () =>
            Arguments.ShouldContain(@" /xml ""xml.xml""");

        It should_include_the_expected_XML_v1_reporting_argument = () =>
            Arguments.ShouldContain(@" /nunit ""nunit.xml""");
    }

    internal class When_using_parameters_which_request_non_default_flags
        : BuildArgumentsSpecsBase
    {
        Establish context = () =>
        {
            Parameters = new XUnit.XUnitParams(
                XUnit.XUnitDefaults.ToolPath,
                XUnit.XUnitDefaults.HtmlOutputPath,
                XUnit.XUnitDefaults.NUnitXmlOutputPath,
                XUnit.XUnitDefaults.XmlOutputPath,
                XUnit.XUnitDefaults.WorkingDir,
                !XUnit.XUnitDefaults.ShadowCopy,
                !XUnit.XUnitDefaults.Silent,
                XUnit.XUnitDefaults.TimeOut,
                XUnit.XUnitDefaults.ErrorLevel,
                XUnit.XUnitDefaults.IncludeTraits,
                XUnit.XUnitDefaults.ExcludeTraits,
                !XUnit.XUnitDefaults.ForceTeamCity);
        };

        It should_request_no_shadow_copy = () =>
            Arguments.ShouldContain(" /noshadow");

        It should_request_silence = () =>
            Arguments.ShouldContain(" /silent");

        It should_force_TeamCity_output = () =>
            Arguments.ShouldContain(" /teamcity");
    }

    [Subject(typeof(XUnit), "result handling")]
    internal abstract class XUnitResultHandlingSpecsBase
    {
        Because of = () =>
            Exception = Catch.Exception(() => XUnit.ResultHandling.failBuildIfXUnitReportedError(ErrorLevel).Invoke(ErrorCode));

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
            ErrorCode = -12;
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
            ErrorCode = -3;
        };

        It should_not_fail_the_build = () =>
            Exception.ShouldBeNull();
    }

    [Subject(typeof(XUnit), "aggregate result handling")]
    internal abstract class XUnitAggregateResultHandlingSpecsBase
    {
        Because of = () =>
            Exception = Catch.Exception(() => XUnit.ResultHandling.failBuildIfXUnitReportedErrors(ErrorLevel).Invoke(ErrorCodes));

        protected static UnitTestCommon.TestRunnerErrorLevel ErrorLevel;
        protected static Tuple<string, int>[] ErrorCodes;
        protected static Exception Exception;
    }

    internal class When_all_assemblies_return_zero_exit_codes_and_the_task_is_configured_not_to_fail_the_build
        : XUnitAggregateResultHandlingSpecsBase
    {
        Establish context = () =>
        {
            ErrorLevel = UnitTestCommon.TestRunnerErrorLevel.DontFailBuild;
            ErrorCodes = new[] {new Tuple<string, int>("Test", 0), new Tuple<string, int>("Test2", 0), };
        };

        It should_not_fail_the_build = () =>
            Exception.ShouldBeNull();
    }

    internal class When_all_assemblies_return_zero_exit_codes_and_the_task_is_configured_to_fail_on_an_error
        : XUnitAggregateResultHandlingSpecsBase
    {
        Establish context = () =>
        {
            ErrorLevel = UnitTestCommon.TestRunnerErrorLevel.Error;
            ErrorCodes = new[] { new Tuple<string, int>("Test", 0), new Tuple<string, int>("Test2", 0), };
        };

        It should_not_fail_the_build = () =>
            Exception.ShouldBeNull();
    }

    internal class When_all_assemblies_return_zero_exit_codes_and_the_task_is_configured_to_fail_on_the_first_error
        : XUnitAggregateResultHandlingSpecsBase
    {
        Establish context = () =>
        {
            ErrorLevel = UnitTestCommon.TestRunnerErrorLevel.FailOnFirstError;
            ErrorCodes = new[] { new Tuple<string, int>("Test", 0), new Tuple<string, int>("Test2", 0), };
        };

        It should_not_fail_the_build = () =>
            Exception.ShouldBeNull();
    }

    internal class When_any_assembly_returns_a_non_zero_exit_codes_and_the_task_is_configured_not_to_fail_the_build
        : XUnitAggregateResultHandlingSpecsBase
    {
        Establish context = () =>
        {
            ErrorLevel = UnitTestCommon.TestRunnerErrorLevel.DontFailBuild;
            ErrorCodes = new[] { new Tuple<string, int>("Test", 0), new Tuple<string, int>("Test2", 4), };
        };

        It should_not_fail_the_build = () =>
            Exception.ShouldBeNull();
    }

    internal class When_any_assembly_returns_a_non_zero_exit_codes_and_the_task_is_configured_to_fail_on_an_error
        : XUnitAggregateResultHandlingSpecsBase
    {
        Establish context = () =>
        {
            ErrorLevel = UnitTestCommon.TestRunnerErrorLevel.Error;
            ErrorCodes = new[] { new Tuple<string, int>("Test", -32), new Tuple<string, int>("Test2", 0), };
        };

        It should_fail_the_build = () =>
            Exception.ShouldNotBeNull();
    }

    internal class When_any_assembly_returns_a_non_zero_exit_codes_and_the_task_is_configured_to_fail_on_the_first_error
        : XUnitAggregateResultHandlingSpecsBase
    {
        Establish context = () =>
        {
            ErrorLevel = UnitTestCommon.TestRunnerErrorLevel.FailOnFirstError;
            ErrorCodes = new[] { new Tuple<string, int>("Test", -1), new Tuple<string, int>("Test2", 5), };
        };

        It should_fail_the_build = () =>
            Exception.ShouldNotBeNull();
    }

    [Subject(typeof(XUnit), "assembly parameter overriding")]
    internal abstract class RunnerExplodeParametersSpecsBase
    {
        protected static void SetupReportingParameters(string htmlPath, string xmlPath, string nUnitPath)
        {
            InputParams = new XUnit.XUnitParams(
                XUnit.XUnitDefaults.ToolPath,
                FSharpOption<string>.Some(htmlPath),
                FSharpOption<string>.Some(nUnitPath),
                FSharpOption<string>.Some(xmlPath),
                XUnit.XUnitDefaults.WorkingDir,
                XUnit.XUnitDefaults.ShadowCopy,
                XUnit.XUnitDefaults.Silent,
                XUnit.XUnitDefaults.TimeOut,
                XUnit.XUnitDefaults.ErrorLevel,
                XUnit.XUnitDefaults.IncludeTraits,
                XUnit.XUnitDefaults.ExcludeTraits,
                XUnit.XUnitDefaults.ForceTeamCity);
        }

        protected static void SetupErrorLevelParameters(UnitTestCommon.TestRunnerErrorLevel errorLevel)
        {
            InputParams = new XUnit.XUnitParams(
                XUnit.XUnitDefaults.ToolPath,
                XUnit.XUnitDefaults.HtmlOutputPath,
                XUnit.XUnitDefaults.NUnitXmlOutputPath,
                XUnit.XUnitDefaults.XmlOutputPath,
                XUnit.XUnitDefaults.WorkingDir,
                XUnit.XUnitDefaults.ShadowCopy,
                XUnit.XUnitDefaults.Silent,
                XUnit.XUnitDefaults.TimeOut,
                errorLevel,
                XUnit.XUnitDefaults.IncludeTraits,
                XUnit.XUnitDefaults.ExcludeTraits,
                XUnit.XUnitDefaults.ForceTeamCity);
        }

        Establish context = () =>
        {
            InputParams = XUnit.XUnitDefaults;
            Assembly = "test.dll";
        };

        Because of = () =>
            Result = XUnit.overrideAssemblyReportParams(Assembly, InputParams);

        protected static string Assembly;
        protected static XUnit.XUnitParams InputParams;
        protected static XUnit.XUnitParams Result;
    }

    internal class When_overriding_the_default_parameters
        : RunnerExplodeParametersSpecsBase
    {
        It should_not_change_the_someness_of_the_HTML_report_parameter = () =>
            OptionModule.IsSome(Result.HtmlOutputPath).ShouldEqual(OptionModule.IsSome(InputParams.HtmlOutputPath));

        It should_not_change_the_someness_of_the_XML_report_parameter = () =>
            OptionModule.IsSome(Result.XmlOutputPath).ShouldEqual(OptionModule.IsSome(InputParams.XmlOutputPath));

        It should_not_change_the_someness_of_the_NUnit_XML_report_parameter = () =>
            OptionModule.IsSome(Result.NUnitXmlOutputPath).ShouldEqual(OptionModule.IsSome(InputParams.NUnitXmlOutputPath));
    }

    internal class When_overriding_parameters_with_reporting_targets
        : RunnerExplodeParametersSpecsBase
    {
        Establish context = () =>
            SetupReportingParameters("report/html", "./face.xml", "nunit.ext");

        It should_prepend_the_assembly_name_to_the_HTML_report_parameter = () =>
            Result.HtmlOutputPath.Value.ShouldEqual("report" + System.IO.Path.DirectorySeparatorChar + Assembly + ".html");

        It should_prepend_the_assembly_name_to_the_XML_report_parameter = () =>
            Result.XmlOutputPath.Value.ShouldEqual("." + System.IO.Path.DirectorySeparatorChar + Assembly + ".face.xml");

        It should_prepend_the_assembly_name_to_the_NUnit_XML_report_parameter = () =>
            Result.NUnitXmlOutputPath.Value.ShouldEqual(Assembly + ".nunit.ext");
    }

    internal class When_overriding_parameters_with_fail_on_first_error
        : RunnerExplodeParametersSpecsBase
    {
        Establish context = () =>
            SetupErrorLevelParameters(UnitTestCommon.TestRunnerErrorLevel.FailOnFirstError);

        It should_not_change_error_level = () =>
            Result.ErrorLevel.ShouldEqual(InputParams.ErrorLevel);
    }

    internal class When_overriding_parameters_with_error
        : RunnerExplodeParametersSpecsBase
    {
        Establish context = () =>
            SetupErrorLevelParameters(UnitTestCommon.TestRunnerErrorLevel.Error);

        It should_change_error_level_to_dont_fail_build = () =>
            Result.ErrorLevel.ShouldEqual(UnitTestCommon.TestRunnerErrorLevel.DontFailBuild);
    }

    internal class When_overriding_parameters_with_dont_fail_on_error
        : RunnerExplodeParametersSpecsBase
    {
        Establish context = () =>
            SetupErrorLevelParameters(UnitTestCommon.TestRunnerErrorLevel.DontFailBuild);

        It should_not_change_error_level = () =>
            Result.ErrorLevel.ShouldEqual(InputParams.ErrorLevel);
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

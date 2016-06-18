using Fake;
using FSharp.Testing;
using Machine.Specifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Test.FAKECore
{
    [Subject(typeof(ReportGeneratorHelper), "report generator argument construction")]
    internal abstract class BuildReportArgumentsSpecs
    {
        protected static ReportGeneratorHelper.ReportGeneratorParams Parameters;
        protected static IEnumerable<string> Reports;
        protected static string Arguments;

        Establish context = () =>
        {
            Parameters = ReportGeneratorHelper.ReportGeneratorDefaultParams;
            Reports = Enumerable.Empty<string>();
        };

        Because of = () =>
        {
            Arguments = ReportGeneratorHelper.buildReportGeneratorArgs(Parameters, Reports);
        };

        protected static readonly IEnumerable<ReportGeneratorHelper.ReportGeneratorReportType> ReportTypes
            = Enum.GetValues(typeof(ReportGeneratorHelper.ReportGeneratorReportType))
                  .Cast<ReportGeneratorHelper.ReportGeneratorReportType>();

        protected static readonly IEnumerable<string> ReportTypesAsText = ReportTypes.Select(rt => rt.ToString());
    }

    internal class when_executing_with_default_arguments : BuildReportArgumentsSpecs
    {
        It should_use_current_directory_as_target_directory =
            () => Arguments.ShouldContain("-targetdir:" + Directory.GetCurrentDirectory());
        It should_only_use_html_report_type =
            () =>
            {
                Arguments.ShouldContain("-reporttypes:Html");
                foreach (string reportType in ReportTypesAsText.Except(new List<string> { "Html" }))
                {
                    Arguments.ShouldNotContain(reportType);
                }
            };
        It should_not_append_source_dirs = () => Arguments.ShouldNotContain("-sourcedirs:");
        It should_not_append_filters = () => Arguments.ShouldNotContain("-filters:");
        It should_not_append_history_dir = () => Arguments.ShouldNotContain("-historydir:");
        It should_have_a_log_verbosity_of_verbose = () => Arguments.ShouldContain("-verbosity:Verbose");
    }

    internal class when_appending_arguments : BuildReportArgumentsSpecs
    {
        It should_surround_reports_with_quotes = () => ArgumentsWithQuotes.Value.ShouldContain("-reports:");
        It should_surround_target_directory_with_quotes = () => ArgumentsWithQuotes.Value.ShouldContain("-targetdir:");
        It should_not_surround_report_types_with_quotes = () => ArgumentsWithQuotes.Value.ShouldNotContain("-reporttypes:");
        It should_not_surround_verbosity_with_quotes = () => ArgumentsWithQuotes.Value.ShouldNotContain("-verbosity:");

        // Note: Needs to be lazy because the static variable 'Arguments' might not be initialized otherwise
        private static Lazy<string> ArgumentsWithQuotes = new Lazy<string>(() => GetArgumentsWithQuotes());

        private static string GetArgumentsWithQuotes()
        {
            var argumentsInQuotes = from Match match in Regex.Matches(Arguments, "\"([^\"]*)\"")
                                    select match.ToString();

            return string.Join("", argumentsInQuotes);
        }
    }

    internal class when_given_multiple_report_types : BuildReportArgumentsSpecs
    {
        Establish context =
            () => Parameters = Parameters.With(p => p.ReportTypes, ReportTypes.ToFSharpList());

        It should_delimit_report_types_with_semi_colon =
            () => Arguments.ShouldContain("-reporttypes:" + string.Join(";", ReportTypesAsText));
    }

    internal class when_given_multiple_reports : BuildReportArgumentsSpecs
    {
        Establish context =
            () => Reports = new List<string> { "report.xml", "other-report.xml" };

        It should_delimit_reports_with_semi_colon =
            () => Arguments.ShouldContain("-reports:" + string.Join(";", Reports));
    }

    internal class when_given_one_or_more_source_directories : BuildReportArgumentsSpecs
    {
        Establish context =
            () => Parameters = Parameters.With(p => p.SourceDirs, new List<string> { "mydirectory" }.ToFSharpList());

        It should_append_source_directories_with_quotes =
            () => Arguments.ShouldContain("\"-sourcedirs:mydirectory\"");
    }

    internal class when_given_one_or_more_filters : BuildReportArgumentsSpecs
    {
        Establish context =
            () => Parameters = Parameters.With(p => p.Filters, new List<string> { "+Included", "-Excluded" }.ToFSharpList());

        It should_append_filters_with_quotes = () => Arguments.ShouldContain("\"-filters:+Included;-Excluded\"");
    }

    internal class when_given_history_directory : BuildReportArgumentsSpecs
    {
        Establish context =
            () => Parameters = Parameters.With(p => p.HistoryDir, "./history/");

        It should_append_history_dir = () => Arguments.ShouldContain("-historydir:./history/");
    }
}

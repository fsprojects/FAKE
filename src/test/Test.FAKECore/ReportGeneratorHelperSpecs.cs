using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FSharp.Testing;

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

        protected static readonly IEnumerable<string> SupportedReportTypes = new List<string>
        {
            "Html",
            "HtmlSummary",
            "Xml",
            "XmlSummary",
            "Latex",
            "LatexSummary",
            "Badges"
        };

        protected static readonly IEnumerable<ReportGeneratorHelper.ReportGeneratorReportType> testR
            = new List<ReportGeneratorHelper.ReportGeneratorReportType>
            {
                ReportGeneratorHelper.ReportGeneratorReportType.Html,
                ReportGeneratorHelper.ReportGeneratorReportType.HtmlSummary,
                ReportGeneratorHelper.ReportGeneratorReportType.Xml,
                ReportGeneratorHelper.ReportGeneratorReportType.XmlSummary,
                ReportGeneratorHelper.ReportGeneratorReportType.Latex,
                ReportGeneratorHelper.ReportGeneratorReportType.LatexSummary,
                ReportGeneratorHelper.ReportGeneratorReportType.Badges
            };
    }

    internal class when_executing_with_default_arguments : BuildReportArgumentsSpecs
    {
        It should_use_current_directory_as_target_directory =
            () => Arguments.ShouldContain("-targetdir:" + Directory.GetCurrentDirectory());
        It should_only_use_html_report_type =
            () =>
            {
                Arguments.ShouldContain("-reporttypes:Html");
                foreach (string reportType in SupportedReportTypes.Except(new List<string> { "Html" }))
                {
                    Arguments.ShouldNotContain(reportType);
                }
            };
        It should_not_append_source_dirs = () => Arguments.ShouldNotContain("-sourcedirs:");
        It should_not_append_filters = () => Arguments.ShouldNotContain("-filters:");
        It should_have_a_log_verbosity_of_verbose = () => Arguments.ShouldContain("-verbosity:Verbose");
    }

    internal class when_given_multiple_report_types : BuildReportArgumentsSpecs
    {
        Establish context =
            () => Parameters = Parameters.With(p => p.ReportTypes, testR.ToFSharpList());

        It should_delimit_report_types_with_semi_colon =
            () => Arguments.ShouldContain("-reporttypes:" + string.Join(";", SupportedReportTypes));
    }

    internal class when_given_multiple_reports : BuildReportArgumentsSpecs
    {
        Establish context =
            () => Reports = new List<string> { "report.xml", "other-report.xml" };

        It should_delimit_reports_with_semi_colon =
            () => Arguments.ShouldContain("-reports:" + string.Join(";", Reports));
    }
}

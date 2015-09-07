using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Test.FAKECore
{
    internal abstract class BuildReportArgumentsSpecs
    {
        protected static ReportGeneratorHelper.ReportGeneratorParams Parameters;
        protected static FSharpList<string> Reports;
        protected static string Arguments;

        Establish context = () =>
        {
            Parameters = ReportGeneratorHelper.ReportGeneratorDefaultParams;
            Reports = FSharpList<string>.Empty;
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
}

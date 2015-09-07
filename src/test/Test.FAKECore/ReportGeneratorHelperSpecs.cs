using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Collections;
using System.IO;

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
    }

    internal class when_executing_with_default_arguments : BuildReportArgumentsSpecs
    {
        It should_use_current_directory_as_target_directory =
            () => Arguments.ShouldContain(Directory.GetCurrentDirectory());
    }
}

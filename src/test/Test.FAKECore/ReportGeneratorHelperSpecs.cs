using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Collections;

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
        
    }
}

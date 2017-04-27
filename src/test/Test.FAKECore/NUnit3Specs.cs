using System;
using Fake.Testing;
using Machine.Specifications;

namespace Test.FAKECore.Testing.NUnit3Specs
{
    [Subject(typeof(NUnit3), "runner argument construction")]
    internal abstract class BuildArgumentsSpecsBase
    {
        private Establish context = () =>
        {
            Assemblies = new[] {"test1.dll", "test2.dll"};
            Parameters = NUnit3.NUnit3Defaults;
        };

        Because of = () =>
        {
            Arguments = NUnit3.buildNUnit3Args(Parameters, Assemblies);
            Console.WriteLine(Arguments);
        };

        protected static NUnit3.NUnit3Params Parameters;
        protected static string[] Assemblies;
        protected static string Arguments;
    }

    internal class When_using_the_default_parameters
        : BuildArgumentsSpecsBase
    {
        It should_not_set_any_trace_value = () =>
        {
            Arguments.ShouldNotContain("--trace");
        };
    }

    internal class When_using_non_default_trace_parameter
        : BuildArgumentsSpecsBase
    {
        Establish context = () =>
        {
            Parameters = new NUnit3.NUnit3Params(
                NUnit3.NUnit3Defaults.ToolPath,
                NUnit3.NUnit3Defaults.Testlist,
                NUnit3.NUnit3Defaults.Where,
                NUnit3.NUnit3Defaults.Config,
                NUnit3.NUnit3Defaults.ProcessModel,
                NUnit3.NUnit3Defaults.Agents,
                NUnit3.NUnit3Defaults.Domain,
                NUnit3.NUnit3Defaults.Framework,
                NUnit3.NUnit3Defaults.Force32bit,
                NUnit3.NUnit3Defaults.DisposeRunners,
                NUnit3.NUnit3Defaults.TimeOut,
                NUnit3.NUnit3Defaults.Seed,
                NUnit3.NUnit3Defaults.Workers,
                NUnit3.NUnit3Defaults.StopOnError,
                NUnit3.NUnit3Defaults.WorkingDir,
                NUnit3.NUnit3Defaults.OutputDir,
                NUnit3.NUnit3Defaults.ErrorDir,
                NUnit3.NUnit3Defaults.ResultSpecs,
                NUnit3.NUnit3Defaults.ShadowCopy,
                NUnit3.NUnit3Defaults.TeamCity,
                NUnit3.NUnit3Defaults.Labels,
                NUnit3.NUnit3Defaults.ErrorLevel,
                NUnit3.NUnit3TraceLevel.Verbose,
                NUnit3.NUnit3Defaults.Params);
        };

        It should_include_the_expected_trace_argument = () =>
        {
            Arguments.ShouldContain(@"--trace=Verbose");
        };
    }
}

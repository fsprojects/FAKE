using System;
using Fake.Testing;
using FSharp.Testing;
using Machine.Specifications;

namespace Test.FAKECore.Testing.NUnit3Specs
{
    [Subject(typeof(NUnit3), "runner argument construction")]
    internal abstract class BuildArgumentsSpecsBase
    {
        private Establish context = () =>
        {
            Assemblies = new[] { "test1.dll", "test2.dll" };
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
        It should_not_set_any_trace_value =
            () => Arguments.ShouldNotContain("--trace");

        It should_not_skip_non_test_assemblies =
            () => Arguments.ShouldNotContain("--skipnontestassemblies");
    }

    internal class When_using_non_default_trace_parameter
        : BuildArgumentsSpecsBase
    {
        Establish context =
            () => Parameters = Parameters.With(p => p.TraceLevel, NUnit3.NUnit3TraceLevel.Verbose);

        It should_include_the_expected_trace_argument =
            () => Arguments.ShouldContain(@"--trace=Verbose");
    }

    internal class When_using_SkipNonTestAssemblies_parameter
        : BuildArgumentsSpecsBase
    {
        Establish context =
            () => Parameters = Parameters.With(p => p.SkipNonTestAssemblies, true);

        It should_include_the_expected_skipnontestassemblies_argument =
            () => Arguments.ShouldContain(@"--skipnontestassemblies");
    }
}

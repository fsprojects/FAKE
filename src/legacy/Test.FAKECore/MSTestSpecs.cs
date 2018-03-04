using System;
using FSharp.Testing;
using Machine.Specifications;
using Fake;

namespace Test.FAKECore.Testing.MSTestSpecs
{
    [Subject(typeof(MSTest), "runner argument construction")]
    internal abstract class BuildArgumentsSpecsBase
    {
        private Establish context = () =>
        {
            Assembly = "testassembly.dll";
            Parameters = MSTest.MSTestDefaults;
        };

        Because of = () =>
        {
            Arguments = MSTest.buildMSTestArgs(Parameters, Assembly);
            Console.WriteLine(Arguments);
        };

        protected static MSTest.MSTestParams Parameters;
        protected static string Assembly;
        protected static string Arguments;
    }

    [Tags("WindowsOnly")]
    internal class When_using_the_default_parameters
        : BuildArgumentsSpecsBase
    {
        It should_contain_assembly_argument =
            () => Arguments.ShouldContain("/testcontainer:testassembly.dll");

        It should_contain_noisolation_argument =
            () => Arguments.ShouldContain("/noisolation");

        It should_not_contain_category_argument =
            () => Arguments.ShouldNotContain("/category:");

        It should_not_contain_testmetadata_argument =
            () => Arguments.ShouldNotContain("/testmetadata:");

        It should_not_contain_testsettings_argument =
            () => Arguments.ShouldNotContain("/testsettings:");

        It should_not_contain_resultsfile_argument =
            () => Arguments.ShouldNotContain("/resultsfile:");

        It should_not_contain_test_argument =
            () => Arguments.ShouldNotContain("/test:");

        It should_not_contain_detail_argument =
            () => Arguments.ShouldNotContain("/detail:");
    }

    [Tags("WindowsOnly")]
    internal class When_using_Category_parameter
        : BuildArgumentsSpecsBase
    {
        Establish context =
            () => Parameters = Parameters.With(p => p.Category, "SampleCategory");

        It should_include_the_expected_category_argument =
            () => Arguments.ShouldContain("/category:SampleCategory");
    }

    [Tags("WindowsOnly")]
    internal class When_using_Tests_parameter
        : BuildArgumentsSpecsBase
    {
        Establish context =
            () => Parameters = Parameters.With(p => p.Tests, (new[] { "test1", @"testproject32\generic" }).ToFSharpList());

        It should_include_the_expected_test_arguments =
            () => Arguments.ShouldContain("\"/test:test1\" \"/test:testproject32\\generic\"");
    }

    [Tags("WindowsOnly")]
    internal class When_using_Details_parameter
        : BuildArgumentsSpecsBase
    {
        Establish context =
            () => Parameters = Parameters.With(p => p.Details, (new[] { "adapter", "computername", "id" }).ToFSharpList());

        It should_include_the_expected_detail_arguments =
            () => Arguments.ShouldContain("\"/detail:adapter\" \"/detail:computername\" \"/detail:id\"");
    }
}

using System;
using Fake;
using FSharp.Testing;
using Machine.Specifications;

namespace Test.FAKECore.NunitCommonSpecs
{
    [Subject(typeof(NUnitCommon), "runner argument construction")]
    internal abstract class BuildArgumentsSpecsBase
    {
        protected static NUnitCommon.NUnitParams Parameters;
        protected static string[] Assemblies;
        protected static string Arguments;

        Establish context = () =>
        {
            Parameters = NUnitCommon.NUnitDefaults;
            Assemblies = new[] { "test.dll", "other.dll" };
        };

        Because of = () =>
        {
            Arguments = NUnitCommon.buildNUnitdArgs(Parameters, Assemblies);
            Console.WriteLine(Arguments);
        };
    }

    internal class When_using_the_default_parameters
        : BuildArgumentsSpecsBase
    {
        It should_not_disable_shadow_copy =
            () => Arguments.ShouldNotContain("-noshadow");
        It should_not_exclude_category =
           () => Arguments.ShouldNotContain("-exclude:");
        It should_not_include_category =
           () => Arguments.ShouldNotContain("-include:");
        It should_not_select_app_domain =
           () => Arguments.ShouldNotContain("-domain:");
        It should_not_show_logo =
           () => Arguments.ShouldContain("-nologo");
        It should_not_specify_error_out_file =
           () => Arguments.ShouldNotContain("-err:");
        It should_not_specify_fixture =
           () => Arguments.ShouldNotContain("-fixture:");
        It should_not_specify_framework =
           () => Arguments.ShouldNotContain("-framework:");
        It should_not_specify_out_file =
           () => Arguments.ShouldNotContain("-out:");
        It should_not_specify_process_model =
           () => Arguments.ShouldNotContain("-process:");
        It should_not_specify_xslt_transform_file =
           () => Arguments.ShouldNotContain("-transform:");
        It should_not_stop_on_error =
           () => Arguments.ShouldNotContain("-stoponerror");
        It should_not_test_in_new_thread =
           () => Arguments.ShouldNotContain("-nothread");
        It should_show_labels =
           () => Arguments.ShouldContain("-labels");
    }

    internal class When_requesting_no_app_domain
        : BuildArgumentsSpecsBase
    {
        Establish context =
           () => Parameters = Parameters.With(p => p.Domain, NUnitCommon.NUnitDomainModel.NoDomainModel);

        It should_request_no_app_domain =
            () => Arguments.ShouldContain("-domain:None");
    }

    internal class When_requesting_single_app_domain
        : BuildArgumentsSpecsBase
    {
        Establish context =
           () => Parameters = Parameters.With(p => p.Domain, NUnitCommon.NUnitDomainModel.SingleDomainModel);

        It should_request_single_app_domain =
            () => Arguments.ShouldContain("-domain:Single");
    }

    internal class When_requesting_multiple_app_domains
        : BuildArgumentsSpecsBase
    {
        Establish context =
           () => Parameters = Parameters.With(p => p.Domain, NUnitCommon.NUnitDomainModel.MultipleDomainModel);

        It should_request_multiple_app_domains =
            () => Arguments.ShouldContain("-domain:Multiple");
    }
}
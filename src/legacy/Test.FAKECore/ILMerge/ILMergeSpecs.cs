using Fake;
using FSharp.Testing;
using Machine.Specifications;

namespace Test.FAKECore.ILMerge
{
    public class when_creating_ILMerge_default_arguments
    {
        const string OutputDll = @".\build\myoutput.dll";
        const string PrimaryAssembly = "myPrimaryAssembly.dll";
        static ILMergeHelper.ILMergeParams _parameters;
        static string _arguments;

        Establish context =
            () =>
            _parameters =
            ILMergeHelper.ILMergeDefaults
                .Set(p => p.Closed).To(true)
                .Set(p => p.CopyAttributes).To(false);


        Because of =
            () => _arguments = ILMergeHelper.getArguments(OutputDll, PrimaryAssembly, _parameters);


        It should_have_the_right_arguments =
            () => _arguments.ShouldEqual("/out:\".\\build\\myoutput.dll\" /target:\"library\" /closed myPrimaryAssembly.dll");
    }
}
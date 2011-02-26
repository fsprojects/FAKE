using Fake;
using Machine.Specifications;

namespace Test.FAKECore.ILMerge
{
    public class when_creating_ILMerge_default_arguments
    {
        static string _arguments;
        static ILMergeHelper.ILMergeParams _parameters;

        Establish context = () => _parameters = ILMergeHelper.ILMergeDefaults;

        Because of =
            () => _arguments = ILMergeHelper.getArguments(@".\build\myoutput.dll", "myprimaassembly.dll", _parameters);


        It should_have_the_right_arguments;
    }
}
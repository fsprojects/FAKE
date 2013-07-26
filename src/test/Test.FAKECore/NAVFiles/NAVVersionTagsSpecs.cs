using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.NAVFiles
{
    public class CanReplaceVersionInCodeunit1
    {
        static string _navisionObject;
        static string _expectedObject;
        static string _result;

        Establish context = () =>
        {
            _navisionObject = File.ReadAllText(@"NAVFiles\Codeunit_1.txt");
            _expectedObject = File.ReadAllText(@"NAVFiles\Codeunit_1_with_trimmed_MSUWW.txt");
        };

        Because of = () => _result = DynamicsNav.replaceVersionTag("MSUWW", "01.01", _navisionObject);

        It should_replace_the_tag = () => _result.ShouldEqual(_expectedObject);
    }


    public class CanReplaceVersionInReport1095
    {
        static string _navisionObject;
        static string _expectedObject;
        static string _result;

        Establish context = () =>
        {
            _navisionObject = File.ReadAllText(@"NAVFiles\Report_1095.txt");
            _expectedObject = File.ReadAllText(@"NAVFiles\Report_1095_with_trimmed_UEN.txt");
        };

        Because of = () => _result = DynamicsNav.replaceVersionTag("UEN", "", _navisionObject);

        It should_replace_the_tag = () => _result.ShouldEqual(_expectedObject);
    }

    public class DontReplaceVersionInDocuTrigger
    {
        static string _navisionObject;
        static string _expectedObject;
        static string _result;

        Establish context = () =>
        {
            _navisionObject = File.ReadAllText(@"NAVFiles\Form_with_VersionTag_in_DocuTrigger.txt");
            _expectedObject = File.ReadAllText(@"NAVFiles\Form_with_VersionTag_in_DocuTrigger_After.txt");
        };

        Because of = () => _result = DynamicsNav.replaceVersionTag("MCN", "01.02", _navisionObject);

        It should_replace_the_tag = () => _result.ShouldEqual(_expectedObject);
    }


    public class CanFindTagInReport1095
    {
        static string _navisionObject;
        static string _expectedObject;
        static string _result;

        Establish context = () =>
        {
            _navisionObject = File.ReadAllText(@"NAVFiles\Report_1095_with_weird_version.txt");
            _expectedObject = File.ReadAllText(@"NAVFiles\Report_1095_with_weird_version.txt");
        };

        Because of = () => _result = DynamicsNav.replaceVersionTag("NIW", "NIW01.01", _navisionObject);

        It should_find_the_double_replaced_tag = () => _result.ShouldNotContain("NIWNIW01.01");
        It should_find_the_tag = () => _result.ShouldContain("NIW01.01");
    }
}
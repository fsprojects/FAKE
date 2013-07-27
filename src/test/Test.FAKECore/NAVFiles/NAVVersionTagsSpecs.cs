using System.Collections.Generic;
using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.NAVFiles
{
    public class CanDetectRequiredTags
    {
        It should_find_MCN_Tag = () =>
            GetMissingRequiredTags(new[] {"MCN"}, "MCN000,foo").ShouldBeEmpty();


        It should_find_MSUWW_in_tags = () =>
            GetMissingRequiredTags(new[] {"MSUWW"}, "TEST1,MSUWW3").ShouldBeEmpty();


        It should_find_missing_MSUWW = () =>
            GetMissingRequiredTags(new[] {"TEST", "MCN", "MSUWW"}, "TEST1,MCN000,foo")
                .ShouldNotBeEmpty();

        It should_find_missing_comma = () =>
            GetMissingRequiredTags(new[] {"MSUWW"}, "TEST1MSUWW3").ShouldNotBeEmpty();

        static IEnumerable<string> GetMissingRequiredTags(IEnumerable<string> requiredTags, string tagList)
        {
            return DynamicsNav.getMissingRequiredTags(requiredTags, DynamicsNav.splitVersionTags(tagList));
        }
    }

    public class CanDetectInvalidTags
    {
        It should_find_invalid_IssueNo = () =>
            GetInvalidTags(new[] {"P0", "I00"}, "MCN,MSUWW001,I0001,foo").ShouldNotBeEmpty();


        It should_find_invalid_project_tag = () =>
            GetInvalidTags(new[] {"P0"}, "P0001,foo").ShouldNotBeEmpty();


        It should_not_complain_project_suffix = () =>
            GetInvalidTags(new[] {"P0"}, "MCNP000,foo").ShouldBeEmpty();


        static IEnumerable<string> GetInvalidTags(IEnumerable<string> invalidTags, string tagList)
        {
            return DynamicsNav.getInvalidTags(invalidTags, DynamicsNav.splitVersionTags(tagList));
        }
    }


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


    public class DoesNotReplaceTwice
    {
        static string _navisionObject;
        static string _result;

        Establish context = () => _navisionObject = File.ReadAllText(@"NAVFiles\Report_1095_with_weird_version.txt");

        Because of = () => _result = DynamicsNav.replaceVersionTag("NIW", "NIW01.01", _navisionObject);

        It should_find_the_double_replaced_tag = () => _result.ShouldNotContain("NIWNIW01.01");
        It should_find_the_tag = () => _result.ShouldContain("NIW01.01");
    }

    public class CanAddMissingTag
    {
        static string _navisionObject;
        static string _result;

        Establish context = () => _navisionObject = File.ReadAllText(@"NAVFiles\Report_1095_with_weird_version.txt");

        Because of = () => _result = DynamicsNav.replaceVersionTag("LEH", "LEH01.01", _navisionObject);

        It should_find_the__new_tag = () => _result.ShouldContain("LEH01.01");
    }
}
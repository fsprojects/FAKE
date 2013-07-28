using System.Collections.Generic;
using System.IO;
using Fake;
using Machine.Specifications;
using System;

namespace Test.FAKECore.NAVFiles
{
    public class CanDetectRequiredTags
    {
        It should_find_MCN_Tag = () =>
            GetMissingRequiredTags(new[] { "MCN" }, "MCN000,foo").ShouldBeEmpty();


        It should_find_MSUWW_in_tags = () =>
            GetMissingRequiredTags(new[] { "MSUWW" }, "TEST1,MSUWW3").ShouldBeEmpty();


        It should_find_missing_MSUWW = () =>
            GetMissingRequiredTags(new[] { "TEST", "MCN", "MSUWW" }, "TEST1,MCN000,foo")
                .ShouldNotBeEmpty();

        It should_find_missing_comma = () =>
            GetMissingRequiredTags(new[] { "MSUWW" }, "TEST1MSUWW3").ShouldNotBeEmpty();

        static IEnumerable<string> GetMissingRequiredTags(IEnumerable<string> requiredTags, string tagList)
        {
            return DynamicsNav.getMissingRequiredTags(requiredTags, DynamicsNav.splitVersionTags(tagList));
        }
    }

    public class CanDetectInvalidTags
    {
        It should_find_invalid_IssueNo = () =>
            GetInvalidTags(new[] { "P0", "I00" }, "MCN,MSUWW001,I0001,foo").ShouldNotBeEmpty();


        It should_find_invalid_project_tag = () =>
            GetInvalidTags(new[] { "P0" }, "P0001,foo").ShouldNotBeEmpty();


        It should_not_complain_project_suffix = () =>
            GetInvalidTags(new[] { "P0" }, "MCNP000,foo").ShouldBeEmpty();


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

    public class CanCheckTagsInObjectString
    {
        static string _navisionObject;
        static string _navisionObject2;

        Establish context = () =>
        {
            _navisionObject = File.ReadAllText(@"NAVFiles\Form_with_VersionTag_in_DocuTrigger.txt");
            _navisionObject2 = File.ReadAllText(@"NAVFiles\PRETaggedReport_1095.txt");
        };

        It should_find_invalid_MCN_tag = () =>
            Catch.Exception(() => DynamicsNav.checkTagsInObjectString(new string[0], false, new[] { "MCN" }, _navisionObject, "test")).Message
                .ShouldContain("Invalid VersionTag MCN found");


        It should_find_required_MCN_tag = () =>
            DynamicsNav.checkTagsInObjectString(new[] { "MCN" }, false, new string[0], _navisionObject, "test")
                .ShouldNotBeNull();

        It should_not_find_invalid_UEN_tag = () =>
            DynamicsNav.checkTagsInObjectString(new string[0], false, new[] { "UEN" }, _navisionObject, "test")
                .ShouldNotBeNull();

        It should_not_find_required_UEN_tag = () =>
            Catch.Exception(() => DynamicsNav.checkTagsInObjectString(new[] { "UEN" }, false, new string[0], _navisionObject, "test")).Message
                .ShouldContain("Required VersionTag UEN not found");

        It should_accept_PRE_tag_if_allowed = () =>
            DynamicsNav.checkTagsInObjectString(new[] { "UEN" }, true, new string[0], _navisionObject2, "test");

        It should_not_accept_PRE_tag_if_not_allowed = () =>
            Catch.Exception(() => DynamicsNav.checkTagsInObjectString(new[] { "UEN" }, false, new string[0], _navisionObject2, "test")).Message
                .ShouldContain("Required VersionTag UEN not found");
    }

    public class CanCheckTagsInFile
    {
        It should_find_invalid_MCN_tag = () =>
            Catch.Exception(() => DynamicsNav.checkTagsInFile(new string[0], false, new[] { "MCN" }, @"NAVFiles\Form_with_VersionTag_in_DocuTrigger.txt")).Message
                .ShouldContain("Invalid VersionTag MCN found");

        It should_find_required_MCN_tag = () =>
            DynamicsNav.checkTagsInFile(new[] { "MCN" }, false, new string[0], @"NAVFiles\Form_with_VersionTag_in_DocuTrigger.txt")
                .ShouldNotBeNull();
    }

    public class CanReplaceDate
    {
        static string _navisionObject;
        static string _expectedObject;
        static string _result;

        Establish context = () =>
        {
            string result = @"NAVFiles\Codeunit_1_with_Date_changed.txt";
            string original = @"NAVFiles\Codeunit_1.txt";

            _navisionObject = File.ReadAllText(original);
            _expectedObject = File.ReadAllText(result);

        };

        Because of = () =>
           _result = DynamicsNav.replaceDateTimeInString(new DateTime(2010, 1, 1, 12, 0, 0), _navisionObject);

        It should_replace_the_date = () => _result.ShouldEqual(_expectedObject);
    }
}
using System;
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
            return DynamicsNavFile.getMissingRequiredTags(requiredTags, DynamicsNavFile.splitVersionTags(tagList));
        }
    }

    public class CanGetVersionNumbers
    {
        It should_find_version_number = () =>
            DynamicsNavFile.getTagVersionInVersionTagList("VU", "VU2.40.03,NTI.Nienburg,ARC5.10,MCN,NIW,PRE,AUS01").ShouldEqual("2.40.03");

        It should_not_find_version_for_non_existing_tag = () =>
            DynamicsNavFile.getTagVersionInVersionTagList("NAVW1", "VU2.40.03,NTI.Nienburg,ARC5.10,MCN,NIW,PRE,AUS01").ShouldBeEmpty();

        It should_find_the_highest_version_number = () => {
            var sourceCode = File.ReadAllText(@"NAVFiles/Table_3_and_4.txt");
            DynamicsNavFile.getHighestTagVersionInObjects("NAVW1", sourceCode).ShouldEqual("7.10");
        };

        It should_find_the_highest_version_number_in_files = () =>
        {
            var files = new[] { @"NAVFiles/Codeunit_1.txt", @"NAVFiles/Table_3.txt" };
            DynamicsNavFile.getHighestTagVersionInFiles("NAVW1", files).ShouldEqual("7.00");
        };
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
            return DynamicsNavFile.getInvalidTags(invalidTags, DynamicsNavFile.splitVersionTags(tagList));
        }
    }


    public class CanReplaceVersionInCodeunit1
    {
        static string _navisionObject;
        static string _expectedObject;
        static string _result;

        Establish context = () =>
        {
            _navisionObject = File.ReadAllText(@"NAVFiles/Codeunit_1.txt");
            _expectedObject = File.ReadAllText(@"NAVFiles/Codeunit_1_with_trimmed_MSUWW.txt");
        };

        Because of = () => _result = DynamicsNavFile.replaceVersionTag("MSUWW", "01.01", _navisionObject);

        It should_replace_the_tag = () => _result.ShouldEqual(_expectedObject);
    }


    public class CanReplaceVersionInReport1095
    {
        static string _navisionObject;
        static string _expectedObject;
        static string _result;

        Establish context = () =>
        {
            _navisionObject = File.ReadAllText(@"NAVFiles/Report_1095.txt");
            _expectedObject = File.ReadAllText(@"NAVFiles/Report_1095_with_trimmed_UEN.txt");
        };

        Because of = () => _result = DynamicsNavFile.replaceVersionTag("UEN", "", _navisionObject);

        It should_replace_the_tag = () => _result.ShouldEqual(_expectedObject);
    }

    public class DontReplaceVersionInDocuTrigger
    {
        static string _navisionObject;
        static string _expectedObject;
        static string _result;

        Establish context = () =>
        {
            _navisionObject = File.ReadAllText(@"NAVFiles/Form_with_VersionTag_in_DocuTrigger.txt");
            _expectedObject = File.ReadAllText(@"NAVFiles/Form_with_VersionTag_in_DocuTrigger_After.txt");
        };

        Because of = () => _result = DynamicsNavFile.replaceVersionTag("MCN", "01.02", _navisionObject);

        It should_replace_the_tag = () => _result.ShouldEqual(_expectedObject);
    }


    public class DoesNotReplaceTwice
    {
        static string _navisionObject;
        static string _result;

        Establish context = () => _navisionObject = File.ReadAllText(@"NAVFiles/Report_1095_with_weird_version.txt");

        Because of = () => _result = DynamicsNavFile.replaceVersionTag("NIW", "NIW01.01", _navisionObject);

        It should_find_the_double_replaced_tag = () => _result.ShouldNotContain("NIWNIW01.01");
        It should_find_the_tag = () => _result.ShouldContain("NIW01.01");
    }

    public class CanAddMissingTag
    {
        static string _navisionObject;
        static string _result;

        Establish context = () => _navisionObject = File.ReadAllText(@"NAVFiles/Report_1095_with_weird_version.txt");

        Because of = () => _result = DynamicsNavFile.replaceVersionTag("AUS", "01", _navisionObject);

        It should_find_the_new_tag = () => _result.ShouldContain("VU2.40.03,NTI.Nienburg,ARC5.10,MCN,NIW,PRE,AUS01");
    }

    public class CanAddMissingTagInCodeunit
    {
        static string _navisionObject;
        static string _result;

        Establish context = () => _navisionObject = File.ReadAllText(@"NAVFiles/Codeunit_419.txt");

        Because of = () => _result = DynamicsNavFile.replaceVersionTag("AUS", "01", _navisionObject);

        It should_find_the_new_tag = () => _result.ShouldContain("NAVW16.00.01,NAVCH6.00.01,UEN,AUS01");
    }

    public class CanCheckTagsInObjectString
    {
        static string _navisionObject;
        static string _navisionObject2;

        Establish context = () =>
        {
            _navisionObject = File.ReadAllText(@"NAVFiles/Form_with_VersionTag_in_DocuTrigger.txt");
            _navisionObject2 = File.ReadAllText(@"NAVFiles/PRETaggedReport_1095.txt");
        };

        It should_accept_PRE_tag_if_allowed = () =>
            DynamicsNavFile.checkTagsInObjectString(new[] {"UEN"}, true, new string[0], _navisionObject2, "test");

        It should_find_invalid_MCN_tag = () =>
            Catch.Exception(() => DynamicsNavFile.checkTagsInObjectString(new string[0], false, new[] {"MCN"}, _navisionObject, "test")).Message
                .ShouldContain("Invalid VersionTag MCN found");


        It should_find_required_MCN_tag = () =>
            DynamicsNavFile.checkTagsInObjectString(new[] {"MCN"}, false, new string[0], _navisionObject, "test")
                .ShouldNotBeNull();

        It should_not_accept_PRE_tag_if_not_allowed = () =>
            Catch.Exception(() => DynamicsNavFile.checkTagsInObjectString(new[] {"UEN"}, false, new string[0], _navisionObject2, "test")).Message
                .ShouldContain("Required VersionTag UEN not found");

        It should_not_find_invalid_UEN_tag = () =>
            DynamicsNavFile.checkTagsInObjectString(new string[0], false, new[] {"UEN"}, _navisionObject, "test")
                .ShouldNotBeNull();

        It should_not_find_required_UEN_tag = () =>
            Catch.Exception(() => DynamicsNavFile.checkTagsInObjectString(new[] {"UEN"}, false, new string[0], _navisionObject, "test")).Message
                .ShouldContain("Required VersionTag UEN not found");
    }

    public class CanCheckTagsInFile
    {
        It should_find_invalid_MCN_tag = () =>
            Catch.Exception(() => DynamicsNavFile.checkTagsInFile(new string[0], false, new[] {"MCN"}, @"NAVFiles/Form_with_VersionTag_in_DocuTrigger.txt")).Message
                .ShouldContain("Invalid VersionTag MCN found");

        It should_find_required_MCN_tag = () =>
            DynamicsNavFile.checkTagsInFile(new[] {"MCN"}, false, new string[0], @"NAVFiles/Form_with_VersionTag_in_DocuTrigger.txt")
                .ShouldNotBeNull();
    }

    public class CanSetVersionTagsInFolder
    {
        static string _tempFolder;

        Establish context = () =>
        {
            _tempFolder = "./tempData";
            FileHelper.CleanDir(_tempFolder);
            foreach (var file in Directory.EnumerateFiles("NAVFiles"))
            {
                var fi = new FileInfo(file);
                fi.CopyTo(Path.Combine(_tempFolder, fi.Name));
            }
        };

        Because of = () => DynamicsNavFile.setVersionTags(new string[0], true, new string[0], "AUS", "AUS01", false, DateTime.MinValue, Directory.EnumerateFiles(_tempFolder));

        It should_find_the__new_tag = () => File.ReadAllText(@"tempData/Codeunit_419.txt").ShouldContain("Version List=NAVW16.00.01,NAVCH6.00.01,UEN,AUS01;");
    }
}
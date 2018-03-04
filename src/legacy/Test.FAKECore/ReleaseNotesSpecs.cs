using System;
using System.Collections.Generic;
using System.Linq;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Collections;

namespace Test.FAKECore
{
    static class Notes
    {
        public static IEnumerable<string> FromString(string data)
        {
            return data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    public class when_parsing_release_notes
    {
        const string validData = @" 

### New in 1.1.9 (Released 2013/07/21)
* Infer booleans for ints that only manifest 0 and 1.

* Support for partially overriding the Schema in CsvProvider.
* PreferOptionals and SafeMode parameters for CsvProvider.

### New in 1.1.10 (Released 2013/09/12)
* Support for heterogeneous XML attributes.
* Make CsvFile re-entrant. *new*
* Support for compressed HTTP responses. 
* Fix JSON conversion of 0 and 1 to booleans.
* Fix XmlProvider problems with nested elements and elements with same name in different namespaces.

";


        static readonly ReleaseNotesHelper.ReleaseNotes expected = ReleaseNotesHelper.ReleaseNotes.New("1.1.10",
            "1.1.10",
            new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 09, 12)),
            new[]
            {
                "Support for heterogeneous XML attributes.",
                "Make CsvFile re-entrant. *new*",
                "Support for compressed HTTP responses.",
                "Fix JSON conversion of 0 and 1 to booleans.",
                "Fix XmlProvider problems with nested elements and elements with same name in different namespaces."
            }
                .ToFSharpList());

        It should_parse_complex =
            () => ReleaseNotesHelper.parseReleaseNotes(Notes.FromString(validData)).ShouldEqual(expected);

        It should_parse_empty_notes =
            () => ReleaseNotesHelper.parseReleaseNotes(Notes.FromString(@"### New in 1.1.10 (Released 2013/09/12)"))
                .ShouldEqual(ReleaseNotesHelper.ReleaseNotes.New("1.1.10", "1.1.10", new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 09, 12)), FSharpList<string>.Empty));

        It should_parse_simple_format =
            () => ReleaseNotesHelper.parseReleaseNotes(Notes.FromString(@"
* 1.1.9 - Infer booleans for ints that only manifest 0 and 1. Support for partially overriding the Schema in CsvProvider.
* 1.1.10 - Support for heterogeneous XML attributes. Make CsvFile re-entrant."))
                .ShouldEqual(ReleaseNotesHelper.ReleaseNotes.New("1.1.10", "1.1.10", null,
                    new[] {"Support for heterogeneous XML attributes.", "Make CsvFile re-entrant."}.ToFSharpList()));


        It should_parse_simple_format_with_dots =
            () => ReleaseNotesHelper.parseReleaseNotes(Notes.FromString(@"
* 1.2.0 - Allow currency symbols on decimals. Remove .AsTuple member from CsvProvider. CsvProvider now uses GetSample instead of constructor like the other providers."))
                .ShouldEqual(ReleaseNotesHelper.ReleaseNotes.New("1.2.0", "1.2.0", null,
                    new[]
                    {
                        "Allow currency symbols on decimals.", "Remove .AsTuple member from CsvProvider.",
                        "CsvProvider now uses GetSample instead of constructor like the other providers."
                    }.ToFSharpList()));

        It should_throws_on_empty_seq_input =
            () => Catch.Exception(() => ReleaseNotesHelper.parseReleaseNotes(new string[] {}));

        It should_throws_on_null_input =
            () => Catch.Exception(() => ReleaseNotesHelper.parseReleaseNotes(null));

        It should_throws_on_single_empty_string_input =
            () => Catch.Exception(() => ReleaseNotesHelper.parseReleaseNotes(new[] {""}));

        It should_throws_on_wrong_formatted_data =
            () => Catch.Exception(() =>
                ReleaseNotesHelper.parseReleaseNotes(Notes.FromString(@"New in 1.1.10 (Released 2013/09/12)")));
    }

    public class when_parsing_many_alpha_versions
    {
        const string input = @"
#### 2.0.0-alpha - December 15 2013
* A

#### 2.0.0-alpha2 - December 24 2013
* B

#### 2.0.0-alpha001 - December 15 2013
* A";
        static readonly ReleaseNotesHelper.ReleaseNotes expected =
            ReleaseNotesHelper.ReleaseNotes.New("2.0.0", "2.0.0-alpha2", null, new[] { "B" }.ToFSharpList());

        It should_parse =
           () => ReleaseNotesHelper.parseReleaseNotes(Notes.FromString(input)).ShouldEqual(expected);
    }

    public class when_parsing_many_pre_release_versions
    {
        const string input = @"
#### 2.0.0-alpha - December 15 2013
* A

#### 2.0.0-rc - December 24 2013
* A

#### 2.0.0-beta - December 24 2013
* A

#### 2.0.0-rc01 - December 24 2013
* A

#### 2.0.0-rc2 - December 24 2013
* A

#### 2.0.0-alpha2 - December 24 2013
* A

#### 2.0.0-rc007 - December 24 2013
* A

#### 2.0.0-alpha001 - December 15 2013
* A";
        static readonly ReleaseNotesHelper.ReleaseNotes expected =
            ReleaseNotesHelper.ReleaseNotes.New("2.0.0", "2.0.0-rc2", null, new[] { "A" }.ToFSharpList());

        It should_parse =
           () => ReleaseNotesHelper.parseReleaseNotes(Notes.FromString(input)).ShouldEqual(expected);
    }

    public class when_parsing_release_notes_in_reverse_order
    {
        const string validData = @" 

### New in 1.1.10 (Released 2013/09/12)
* Support for heterogeneous XML attributes.
* Make CsvFile re-entrant. 
* Support for compressed HTTP responses. 
* Fix JSON conversion of 0 and 1 to booleans.
* Fix XmlProvider problems with nested elements and elements with same name in different namespaces.

### New in 1.0.9 (Released 2013/07/21)
* Infer booleans for ints that only manifest 0 and 1.

* Support for partially overriding the Schema in CsvProvider.
* PreferOptionals and SafeMode parameters for CsvProvider.

";


        static readonly ReleaseNotesHelper.ReleaseNotes expected = ReleaseNotesHelper.ReleaseNotes.New("1.1.10",
            "1.1.10",
            new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 09, 12)),
            new[]
            {
                "Support for heterogeneous XML attributes.",
                "Make CsvFile re-entrant.",
                "Support for compressed HTTP responses.",
                "Fix JSON conversion of 0 and 1 to booleans.",
                "Fix XmlProvider problems with nested elements and elements with same name in different namespaces."
            }
                .ToFSharpList());

        It should_parse_complex =
            () => ReleaseNotesHelper.parseReleaseNotes(Notes.FromString(validData)).ShouldEqual(expected);

        It should_parse_empty_notes =
            () => ReleaseNotesHelper.parseReleaseNotes(Notes.FromString(@"### New in 1.1.10 (Released 2013/09/12)"))
                .ShouldEqual(ReleaseNotesHelper.ReleaseNotes.New("1.1.10", "1.1.10", new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 09, 12)), FSharpList<string>.Empty));

        It should_parse_simple_format =
            () => ReleaseNotesHelper.parseReleaseNotes(Notes.FromString(@"
* 1.1.10 - Support for heterogeneous XML attributes. Make CsvFile re-entrant.
* 1.0.9 - Infer booleans for ints that only manifest 0 and 1. Support for partially overriding the Schema in CsvProvider."))
                .ShouldEqual(ReleaseNotesHelper.ReleaseNotes.New("1.1.10", "1.1.10", null,
                    new[] {"Support for heterogeneous XML attributes.", "Make CsvFile re-entrant."}.ToFSharpList()));

        It should_throws_on_empty_seq_input =
            () => Catch.Exception(() => ReleaseNotesHelper.parseReleaseNotes(new string[] {}));

        It should_throws_on_null_input =
            () => Catch.Exception(() => ReleaseNotesHelper.parseReleaseNotes(null));

        It should_throws_on_single_empty_string_input =
            () => Catch.Exception(() => ReleaseNotesHelper.parseReleaseNotes(new[] {""}));

        It should_throws_on_wrong_formatted_data =
            () => Catch.Exception(() =>
                ReleaseNotesHelper.parseReleaseNotes(Notes.FromString(@"New in 1.1.10 (Released 2013/09/12)")));
    }

    public class when_parsing_all_release_notes
    {
        const string validData = @" 

### New in 1.1.9 (Released 2013/07/21)
* Infer booleans for ints that only manifest 0 and 1.
### New in 1.1.10 (Released 2013/09/12)
* Support for heterogeneous XML attributes.
* Make CsvFile re-entrant. 
* Support for compressed HTTP responses. 
* Fix JSON conversion of 0 and 1 to booleans.
* Fix XmlProvider problems with nested elements and elements with same name in different namespaces.

";


        static readonly ReleaseNotesHelper.ReleaseNotes expected = ReleaseNotesHelper.ReleaseNotes.New("1.1.10",
            "1.1.10",
            new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 09, 12)),
            new[]
            {
                "Support for heterogeneous XML attributes.",
                "Make CsvFile re-entrant.",
                "Support for compressed HTTP responses.",
                "Fix JSON conversion of 0 and 1 to booleans.",
                "Fix XmlProvider problems with nested elements and elements with same name in different namespaces."
            }
                .ToFSharpList());

        static readonly ReleaseNotesHelper.ReleaseNotes expected2 = ReleaseNotesHelper.ReleaseNotes.New("1.1.9", "1.1.9",
            new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 07, 21)),
            new[] {"Infer booleans for ints that only manifest 0 and 1."}
                .ToFSharpList());

        static FSharpList<ReleaseNotesHelper.ReleaseNotes> _result;

        Because of = () => _result = ReleaseNotesHelper.parseAllReleaseNotes(Notes.FromString(validData));

        It should_find_both_entries =
            () => _result.Length.ShouldEqual(2);

        It should_find_the_first_entry =
            () => _result.First().ShouldEqual(expected);

        It should_find_the_second_entry =
            () => _result.Skip(1).First().ShouldEqual(expected2);
    }

    public class when_parsing_octokit_release_notes
    {
        static readonly IEnumerable<string> Notes = StringHelper.ReadFile("ReleaseNotes/Octokit.md");


        static readonly ReleaseNotesHelper.ReleaseNotes expected = ReleaseNotesHelper.ReleaseNotes.New("0.1.3", "0.1.3",
            new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 11, 5)),
            new[]
            {
                "New Xamarin Component store versions of Octokit.net",
                "New clients for managing assignees, milestones, and tags",
                "New clients for managing issues, issue events, and issue comments",
                "New client for managing organization members",
                "Fixed bug in applying query parameters that could cause paging to continually request the same page"
            }
                .ToFSharpList());

        static FSharpList<ReleaseNotesHelper.ReleaseNotes> _result;

        Because of = () => _result = ReleaseNotesHelper.parseAllReleaseNotes(Notes);

        It should_find_all_entries = () => _result.Length.ShouldEqual(4);

        It should_find_the_first_entry =
            () => _result.First().ShouldEqual(expected);
    }

    public class when_parsing_machine_release_notes
    {
        static readonly IEnumerable<string> Notes = StringHelper.ReadFile("ReleaseNotes/Machine.md");


        static readonly ReleaseNotesHelper.ReleaseNotes expected = ReleaseNotesHelper.ReleaseNotes.New("1.8.0", "1.8.0",
            null,
            new[]
            {
                "When the subject's constructor throws an exception, it is now bubbled up and shown in the failure message.",
                "Fixed an exception in the Rhino Mocks adapter when faking a delegate (thanks to [Alexis Atkinson](https://github.com/alexisatkinson)).",
                "Updated to FakeItEasy 1.14.0",
                "Updated to Machine.Specifications 0.5.16",
                "Updated to Moq 4.1.1309.1617"
            }
                .ToFSharpList());

        static FSharpList<ReleaseNotesHelper.ReleaseNotes> _result;

        Because of = () => _result = ReleaseNotesHelper.parseAllReleaseNotes(Notes);

        It should_find_all_entries = () => _result.Length.ShouldEqual(17);

        It should_find_the_first_entry =
            () => _result.First().ShouldEqual(expected);
    }

    public class when_parsing_semver_release_notes
    {
        static readonly IEnumerable<string> Notes = StringHelper.ReadFile("ReleaseNotes/SemVer.md");


        static readonly ReleaseNotesHelper.ReleaseNotes expected =
            ReleaseNotesHelper.ReleaseNotes.New("1.0.0",
                "1.0.0-rc.1",
                new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 10, 30)),
                new[]
                {
                    "Initial release"
                }
                    .ToFSharpList());

        static readonly ReleaseNotesHelper.ReleaseNotes expected2 =
            ReleaseNotesHelper.ReleaseNotes.New("1.0.0",
                "1.0.0-beta.2",
                null,
                new[]
                {
                    "Fixed problems with Microsoft.Threading.Tasks"
                }
                    .ToFSharpList());

        static FSharpList<ReleaseNotesHelper.ReleaseNotes> _result;

        Because of = () => _result = ReleaseNotesHelper.parseAllReleaseNotes(Notes);

        It should_find_all_entries = () => _result.Length.ShouldEqual(2);

        It should_find_the_first_entry =
            () => _result.First().ShouldEqual(expected);

        It should_find_the_second_entry =
            () => _result.Skip(1).First().ShouldEqual(expected2);
    }
}
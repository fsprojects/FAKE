using System;
using System.Collections.Generic;
using System.Linq;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Collections;

namespace Test.FAKECore
{
    static class Changes
    {
        public static IEnumerable<string> FromString(string data)
        {
            return data.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        }
    }

    public class when_parsing_change_log
    {
        private const string validData = @" 
# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/) 
and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]
### Added
- Line 1 Added (unreleased)
- Line 2 Added (unreleased)

### Changed
- Line 1 Changed (unreleased)
- Line 2 Changed (unreleased)

## [0.3.0] - 2015-12-03

This is a description
Description Line 2

### Added
- Line 1 0.3.0 Added
- Line 2 0.3.0 Added

### Fixed
- Line 1 0.3.0 Fixed
- Line 2 0.3.0 Fixed

## 0.2.0-beta1 - 2015-10-06
### Changed
- Line 1 0.2.0-beta1 Changed
- Line 2 0.2.0-beta1 Changed

[Unreleased]: https://github.com/olivierlacan/keep-a-changelog/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/olivierlacan/keep-a-changelog/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/olivierlacan/keep-a-changelog/compare/v0.1.0...v0.2.0
What’s the point of a change log?";

        private static readonly ChangeLogHelper.ChangeLogEntry expected = ChangeLogHelper.ChangeLogEntry.New(
            "0.3.0",
            "0.3.0",
            new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2015, 12, 03)),
            new Microsoft.FSharp.Core.FSharpOption<string>("This is a description\nDescription Line 2"),
            new ChangeLogHelper.Change[]
            {
                ChangeLogHelper.Change.NewAdded("Line 1 0.3.0 Added"),
                ChangeLogHelper.Change.NewAdded("Line 2 0.3.0 Added"),
                ChangeLogHelper.Change.NewFixed("Line 1 0.3.0 Fixed"),
                ChangeLogHelper.Change.NewFixed("Line 2 0.3.0 Fixed"),
            }.ToFSharpList(),
            false);

        It should_parse =
            () => ChangeLogHelper.parseChangeLog(Changes.FromString(validData)).LatestEntry.ShouldEqual(expected);

        It should_parse_empty_changes =
            () => ChangeLogHelper.parseChangeLog(Changes.FromString("# Change log\n\n## [0.3.0] - 2015-12-03")).LatestEntry
                .ShouldEqual(ChangeLogHelper.ChangeLogEntry.New("0.3.0", "0.3.0", new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2015, 12, 03)), Microsoft.FSharp.Core.FSharpOption<string>.None, FSharpList<ChangeLogHelper.Change>.Empty, false));

        It should_throw_if_top_level_header_is_missing =
            () => Catch.Exception(() => ChangeLogHelper.parseChangeLog(Changes.FromString(@"## [0.3.0] - 2015-12-03")));

        It should_throw_if_top_level_header_is_not_first_non_empty_line =
            () => Catch.Exception(() => ChangeLogHelper.parseChangeLog(Changes.FromString("FOO\n\n# Change log\n\n## [0.3.0] - 2015-12-03")));

        It should_throw_on_empty_seq_input =
            () => Catch.Exception(() => ChangeLogHelper.parseChangeLog(new string[] { }));

        It should_throw_on_null_input =
            () => Catch.Exception(() => ChangeLogHelper.parseChangeLog(null));

        It should_throw_on_single_empty_string_input =
            () => Catch.Exception(() => ChangeLogHelper.parseChangeLog(new[] { "" }));
    }

    public class when_parsing_custom_categories
    {
        const string validData = @"
# Changelog

## [2.0.0] - 2013-12-15
### Configuration
* The Configuration has changed

";

        private static readonly ChangeLogHelper.ChangeLogEntry expected = ChangeLogHelper.ChangeLogEntry.New(
            "2.0.0",
            "2.0.0",
            new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 12, 15)),
            Microsoft.FSharp.Core.FSharpOption<string>.None,
            new ChangeLogHelper.Change[]
            {
                ChangeLogHelper.Change.NewCustom("Configuration", "The Configuration has changed"),
            }.ToFSharpList(),
            false);

        It should_parse = 
            () => ChangeLogHelper.parseChangeLog(Changes.FromString(validData)).LatestEntry.ShouldEqual(expected);
    }

    public class when_parsing_yanked_entries
    {
        const string validData = @"
# Changelog

## [2.0.0] - 2013-12-15
### Added
* A

## [1.0.0] - 2013-12-14 [YANKED]
### Added
* B

";
        private static readonly ChangeLogHelper.ChangeLogEntry expected = ChangeLogHelper.ChangeLogEntry.New(
            "2.0.0",
            "2.0.0",
            new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 12, 15)),
            Microsoft.FSharp.Core.FSharpOption<string>.None,
            new ChangeLogHelper.Change[] { ChangeLogHelper.Change.NewAdded("A") }.ToFSharpList(),
            false);

        private static readonly ChangeLogHelper.ChangeLogEntry expected2 = ChangeLogHelper.ChangeLogEntry.New(
            "1.0.0",
            "1.0.0",
            new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 12, 14)),
            Microsoft.FSharp.Core.FSharpOption<string>.None,
            new ChangeLogHelper.Change[] { ChangeLogHelper.Change.NewAdded("B") }.ToFSharpList(),
            true);

        static ChangeLogHelper.ChangeLog _result;

        Because of = () =>  _result = ChangeLogHelper.parseChangeLog(Changes.FromString(validData));

        It should_parse_the_first_entry_as_not_yanked = 
            () => _result.Entries.First().ShouldEqual(expected);

        It should_parse_the_second_entry_as_yanked =
            () => _result.Entries.Skip(1).First().ShouldEqual(expected2);
    }

    public class when_parsing_many_alpha_versions_in_change_log
    {
        const string input = @"
# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/) 
and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]

## [2.0.0-alpha] - 2013-12-15
### Added
* A

## 2.0.0-alpha2 - 2013-12-24
### Added
* B

## 2.0.0-alpha001 - 2013-12-15
### Added
* A";

        static readonly ChangeLogHelper.ChangeLogEntry expected =
            ChangeLogHelper.ChangeLogEntry.New(
                "2.0.0",
                "2.0.0-alpha2",
                new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 12, 24)),
                Microsoft.FSharp.Core.FSharpOption<string>.None,
                new ChangeLogHelper.Change[] { ChangeLogHelper.Change.NewAdded("B") }.ToFSharpList(),
                false);

        It should_parse =
           () => ChangeLogHelper.parseChangeLog(Changes.FromString(input)).LatestEntry.ShouldEqual(expected);
    }

    public class when_parsing_all_changes
    {
        const string validData = @" 

# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/) 
and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]

This is a description
Description Line 2

### Added
- Unreleased added

### Changed
- Unreleased changed

### Deprecated
- Unreleased deprecated

### Removed
- Unreleased removed

### Fixed
- Unreleased fixed

### Security
- Unreleased security

## [1.1.9] - 2013-07-21
### Changed
- Infer booleans for ints that only manifest 0 and 1.

## [1.1.10] - 2013-09-12
### Added
- Support for heterogeneous XML attributes.
- Make CsvFile re-entrant. 
- Support for compressed HTTP responses. 

### Fixed
- Fix JSON conversion of 0 and 1 to booleans.
- Fix XmlProvider problems with nested elements and elements with same name in different namespaces.

";

        static readonly ChangeLogHelper.ChangeLogEntry expected = ChangeLogHelper.ChangeLogEntry.New(
            "1.1.10",
            "1.1.10",
            new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 09, 12)),
            Microsoft.FSharp.Core.FSharpOption<string>.None,
            new ChangeLogHelper.Change[]
            {
                ChangeLogHelper.Change.NewAdded("Support for heterogeneous XML attributes."),
                ChangeLogHelper.Change.NewAdded("Make CsvFile re-entrant."),
                ChangeLogHelper.Change.NewAdded("Support for compressed HTTP responses."),
                ChangeLogHelper.Change.NewFixed("Fix JSON conversion of 0 and 1 to booleans."),
                ChangeLogHelper.Change.NewFixed("Fix XmlProvider problems with nested elements and elements with same name in different namespaces.")
            }.ToFSharpList(),
            false);

        static readonly ChangeLogHelper.ChangeLogEntry expected2 = ChangeLogHelper.ChangeLogEntry.New(
            "1.1.9",
            "1.1.9",
            new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 07, 21)),
            Microsoft.FSharp.Core.FSharpOption<string>.None,
            new ChangeLogHelper.Change[] { ChangeLogHelper.Change.NewChanged("Infer booleans for ints that only manifest 0 and 1.") }.ToFSharpList(),
            false);

        private static readonly Microsoft.FSharp.Core.FSharpOption<ChangeLogHelper.Unreleased> expectedUnreleased =
            ChangeLogHelper.Unreleased.New(
                new Microsoft.FSharp.Core.FSharpOption<string>("This is a description\nDescription Line 2"),
                new ChangeLogHelper.Change[]
                {
                    ChangeLogHelper.Change.NewAdded("Unreleased added"),
                    ChangeLogHelper.Change.NewChanged("Unreleased changed"),
                    ChangeLogHelper.Change.NewDeprecated("Unreleased deprecated"),
                    ChangeLogHelper.Change.NewRemoved("Unreleased removed"),
                    ChangeLogHelper.Change.NewFixed("Unreleased fixed"),
                    ChangeLogHelper.Change.NewSecurity("Unreleased security")
                }.ToFSharpList());

        static readonly Microsoft.FSharp.Core.FSharpOption<string> expectedDescription = 
            new Microsoft.FSharp.Core.FSharpOption<string>("All notable changes to this project will be documented in this file.\n\nThe format is based on [Keep a Changelog](http://keepachangelog.com/)\nand this project adheres to [Semantic Versioning](http://semver.org/).");

        const string expectedHeader = "Change Log";
        
        static ChangeLogHelper.ChangeLog _result;

        Because of = () =>  _result = ChangeLogHelper.parseChangeLog(Changes.FromString(validData));

        It should_parse_the_correct_header_line =
            () => _result.Header.ShouldEqual(expectedHeader);

        It should_find_both_entries =
            () => _result.Entries.Length.ShouldEqual(2);

        It should_find_the_latest_entry =
            () => _result.LatestEntry.ShouldEqual(expected);

        It should_find_the_first_entry =
            () => _result.Entries.First().ShouldEqual(expected);

        It should_find_the_second_entry =
            () => _result.Entries.Skip(1).First().ShouldEqual(expected2);

        It should_parse_the_unreleased_entries =
            () => _result.Unreleased.ShouldEqual(expectedUnreleased);

        It should_parse_the_description =
            () => _result.Description.ShouldEqual(expectedDescription);
    }

    public class when_deserializing_a_ChangeLog_entry
    {
        private static string Normalize(string value)
        {
            return value
                .Replace(Environment.NewLine, @"\n")
                .Replace("\r\n", @"\n")
                .Replace("\n\r", @"\n")
                .Replace("\r", @"\n")
                .Replace("\n", @"\n");
        }

        private It should_return_minimal_header_if_minimal_info_is_given = () =>
            {
                var entry = ChangeLogHelper.ChangeLogEntry.New(
                    "0.3.0.1",
                    "0.3.0-beta1",
                    FSharpList<ChangeLogHelper.Change>.Empty);

                entry.ToString().ShouldEqual("## 0.3.0-beta1");
            };

        private It should_return_header_with_date_if_date_is_given = () =>
            {
                var entry = ChangeLogHelper.ChangeLogEntry.New(
                    "0.3.0.1",
                    "0.3.0-beta1",
                    new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 09, 12)),
                    Microsoft.FSharp.Core.FSharpOption<string>.None,
                    FSharpList<ChangeLogHelper.Change>.Empty,
                    false);

                entry.ToString().ShouldEqual("## 0.3.0-beta1 - 2013-09-12");
            };

        private It should_return_header_with_date_and_YANKED_marker_if_date_is_given_and_IsYanked_is_true = () =>
            {
                var entry = ChangeLogHelper.ChangeLogEntry.New(
                    "0.3.0.1",
                    "0.3.0-beta1",
                    new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 09, 12)),
                    Microsoft.FSharp.Core.FSharpOption<string>.None,
                    FSharpList<ChangeLogHelper.Change>.Empty,
                    true);

                entry.ToString().ShouldEqual("## 0.3.0-beta1 - 2013-09-12 [YANKED]");
            };

        private It should_return_header_without_date_but_with_YANKED_marker_if_date_is_not_given_but_IsYanked_is_true =
            () =>
                {
                    var entry = ChangeLogHelper.ChangeLogEntry.New(
                        "0.3.0.1",
                        "0.3.0-beta1",
                        Microsoft.FSharp.Core.FSharpOption<DateTime>.None,
                        Microsoft.FSharp.Core.FSharpOption<string>.None,
                        FSharpList<ChangeLogHelper.Change>.Empty,
                        true);

                    entry.ToString().ShouldEqual("## 0.3.0-beta1 [YANKED]");
                };

        private It should_return_header_and_description_if_description_is_set = () =>
            {
                var entry = ChangeLogHelper.ChangeLogEntry.New(
                    "0.3.0.1",
                    "0.3.0-beta1",
                    Microsoft.FSharp.Core.FSharpOption<DateTime>.None,
                    new Microsoft.FSharp.Core.FSharpOption<string>("This is a description"),
                    FSharpList<ChangeLogHelper.Change>.Empty,
                    false);

                Normalize(entry.ToString()).ShouldEqual(Normalize("## 0.3.0-beta1\n\nThis is a description"));
            };

        private It should_return_changes_grouped_by_header = () =>
            {
                var changes =
                    new ChangeLogHelper.Change[]
                            {
                                ChangeLogHelper.Change.NewAdded("added 1"),
                                ChangeLogHelper.Change.NewChanged("changed 1"),
                                ChangeLogHelper.Change.NewDeprecated("deprecated 1"),
                                ChangeLogHelper.Change.NewRemoved("removed 1"),
                                ChangeLogHelper.Change.NewFixed("fixed 1"),
                                ChangeLogHelper.Change.NewSecurity("security 1"),
                                ChangeLogHelper.Change.NewCustom("MyCustomHeader", "custom 1"),
                                ChangeLogHelper.Change.NewAdded("added 2"),
                                ChangeLogHelper.Change.NewChanged("changed 2"),
                                ChangeLogHelper.Change.NewDeprecated("deprecated 2"),
                                ChangeLogHelper.Change.NewRemoved("removed 2"),
                                ChangeLogHelper.Change.NewFixed("fixed 2"),
                                ChangeLogHelper.Change.NewSecurity("security 2"),
                                ChangeLogHelper.Change.NewCustom("MyCustomHeader", "custom 2")
                            }
                        .ToFSharpList();

                var entry = ChangeLogHelper.ChangeLogEntry.New(
                    "0.3.0.1",
                    "0.3.0-beta1",
                    new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 09, 12)),
                    new Microsoft.FSharp.Core.FSharpOption<string>("This is a description"),
                    changes,
                    true);

                const string expected =
@"## 0.3.0-beta1 - 2013-09-12 [YANKED]

This is a description

### Added
- added 1
- added 2

### Changed
- changed 1
- changed 2

### Deprecated
- deprecated 1
- deprecated 2

### Removed
- removed 1
- removed 2

### Fixed
- fixed 1
- fixed 2

### Security
- security 1
- security 2

### MyCustomHeader
- custom 1
- custom 2";

                Normalize(entry.ToString()).ShouldEqual(Normalize(expected));
            };

        private It should_have_correct_line_feeds_if_description_is_missing = () =>
            {
                var entry = ChangeLogHelper.ChangeLogEntry.New(
                    "0.3.0.1",
                    "0.3.0-beta1",
                    new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 09, 12)),
                    Microsoft.FSharp.Core.FSharpOption<string>.None,
                    new ChangeLogHelper.Change[] { ChangeLogHelper.Change.NewAdded("added 1") }.ToFSharpList(),
                    true);

                const string expected =
@"## 0.3.0-beta1 - 2013-09-12 [YANKED]

### Added
- added 1";

                Normalize(entry.ToString()).ShouldEqual(Normalize(expected));
            };
    }

    public class when_deserializing_the_Unreleased_section
    {
        private static string Normalize(string value)
        {
            return value
                .Replace(Environment.NewLine, @"\n")
                .Replace("\r\n", @"\n")
                .Replace("\n\r", @"\n")
                .Replace("\r", @"\n")
                .Replace("\n", @"\n");
        }

        private It should_return_header_and_description_if_description_is_set = () =>
        {
            var entry = ChangeLogHelper.Unreleased.New(
                new Microsoft.FSharp.Core.FSharpOption<string>("This is a description"),
                FSharpList<ChangeLogHelper.Change>.Empty);

            Normalize(entry.Value.ToString()).ShouldEqual(Normalize("## Unreleased\n\nThis is a description"));
        };

        private It should_return_changes_grouped_by_header = () =>
        {
            var changes = new ChangeLogHelper.Change[]
            {
                ChangeLogHelper.Change.NewAdded("added 1"),
                ChangeLogHelper.Change.NewChanged("changed 1"),
                ChangeLogHelper.Change.NewDeprecated("deprecated 1"),
                ChangeLogHelper.Change.NewRemoved("removed 1"),
                ChangeLogHelper.Change.NewFixed("fixed 1"),
                ChangeLogHelper.Change.NewSecurity("security 1"),
                ChangeLogHelper.Change.NewCustom("MyCustomHeader", "custom 1"),
                ChangeLogHelper.Change.NewAdded("added 2"),
                ChangeLogHelper.Change.NewChanged("changed 2"),
                ChangeLogHelper.Change.NewDeprecated("deprecated 2"),
                ChangeLogHelper.Change.NewRemoved("removed 2"),
                ChangeLogHelper.Change.NewFixed("fixed 2"),
                ChangeLogHelper.Change.NewSecurity("security 2"),
                ChangeLogHelper.Change.NewCustom("MyCustomHeader", "custom 2")
            }.ToFSharpList();

            var entry = ChangeLogHelper.Unreleased.New(
                new Microsoft.FSharp.Core.FSharpOption<string>("This is a description"),
                changes);

            const string expected =
@"## Unreleased

This is a description

### Added
- added 1
- added 2

### Changed
- changed 1
- changed 2

### Deprecated
- deprecated 1
- deprecated 2

### Removed
- removed 1
- removed 2

### Fixed
- fixed 1
- fixed 2

### Security
- security 1
- security 2

### MyCustomHeader
- custom 1
- custom 2";

            Normalize(entry.Value.ToString()).ShouldEqual(Normalize(expected));
        };

        private It should_have_correct_line_feeds_if_description_is_missing = () =>
        {
            var entry = ChangeLogHelper.Unreleased.New(
                Microsoft.FSharp.Core.FSharpOption<string>.None,
                new ChangeLogHelper.Change[] { ChangeLogHelper.Change.NewAdded("added 1") }.ToFSharpList());

            const string expected =
@"## Unreleased

### Added
- added 1";

            Normalize(entry.Value.ToString()).ShouldEqual(Normalize(expected));
        };
    }

    public class when_deserializing_a_ChangeLog
    {
        private static string Normalize(string value)
        {
            return value
                .Replace(Environment.NewLine, @"\n")
                .Replace("\r\n", @"\n")
                .Replace("\n\r", @"\n")
                .Replace("\r", @"\n")
                .Replace("\n", @"\n");
        }

        private static readonly ChangeLogHelper.ChangeLog changeLog =
            ChangeLogHelper.ChangeLog.New(
                "Changelog header",
                new Microsoft.FSharp.Core.FSharpOption<string>("This is a description"),
                ChangeLogHelper.Unreleased.New(
                    new Microsoft.FSharp.Core.FSharpOption<string>("Unreleased description"),
                    new ChangeLogHelper.Change[]
                    {
                        ChangeLogHelper.Change.NewAdded("Unreleased added")
                    }.ToFSharpList()),
                new ChangeLogHelper.ChangeLogEntry[]
                {
                    ChangeLogHelper.ChangeLogEntry.New(
                        "0.0.3",
                        "0.0.3",
                        new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2014, 12, 13)),
                        new Microsoft.FSharp.Core.FSharpOption<string>("Description for 0.0.3"),
                        new ChangeLogHelper.Change[] { ChangeLogHelper.Change.NewFixed("Fixed in 0.0.3") }.ToFSharpList(),
                        false),

                    ChangeLogHelper.ChangeLogEntry.New(
                        "0.0.2",
                        "0.0.2",
                        Microsoft.FSharp.Core.FSharpOption<DateTime>.None,
                        Microsoft.FSharp.Core.FSharpOption<string>.None,
                        new ChangeLogHelper.Change[] { ChangeLogHelper.Change.NewDeprecated("Deprecated in 0.0.2") }.ToFSharpList(),
                        true)
                }.ToFSharpList());

        private const string expected =
@"# Changelog header

This is a description

## Unreleased

Unreleased description

### Added
- Unreleased added

## 0.0.3 - 2014-12-13

Description for 0.0.3

### Fixed
- Fixed in 0.0.3

## 0.0.2 [YANKED]

### Deprecated
- Deprecated in 0.0.2";

        private It should_serialize_correctly = () => Normalize(changeLog.ToString()).ShouldEqual(Normalize(expected));
    }

    public class when_promoting_changes
    {
        private static readonly ChangeLogHelper.ChangeLog before =
            ChangeLogHelper.ChangeLog.New(
                "Changelog header",
                new Microsoft.FSharp.Core.FSharpOption<string>("This is a description"),
                ChangeLogHelper.Unreleased.New(
                    new Microsoft.FSharp.Core.FSharpOption<string>("Unreleased description"),
                    new ChangeLogHelper.Change[]
                    {
                        ChangeLogHelper.Change.NewAdded("Unreleased added")
                    }.ToFSharpList()),
                FSharpList<ChangeLogHelper.ChangeLogEntry>.Empty);

        private static readonly ChangeLogHelper.ChangeLog promoted =
            ChangeLogHelper.ChangeLog.New(
                "Changelog header",
                new Microsoft.FSharp.Core.FSharpOption<string>("This is a description"),
                Microsoft.FSharp.Core.FSharpOption<ChangeLogHelper.Unreleased>.None,
                new ChangeLogHelper.ChangeLogEntry[]
                {
                    ChangeLogHelper.ChangeLogEntry.New(
                        "0.0.3",
                        "0.0.3-beta1",
                        new Microsoft.FSharp.Core.FSharpOption<DateTime>(DateTime.Today),
                        new Microsoft.FSharp.Core.FSharpOption<string>("Unreleased description"),
                        new ChangeLogHelper.Change[] { ChangeLogHelper.Change.NewAdded("Unreleased added") }.ToFSharpList(),
                        false)
                }.ToFSharpList());

        private It should_move_unreleased_entries_to_new_version_on_PromoteUnreleased =
            () => before.PromoteUnreleased("0.0.3-beta1").ShouldEqual(promoted);

        private It should_not_change_on_PromoteUnreleased_if_there_is_no_Unreleased_section =
            () => promoted.PromoteUnreleased("0.0.4-beta1").ShouldEqual(promoted);
    }
}

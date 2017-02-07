using System;
using System.Collections.Generic;
using System.Linq;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Collections;

namespace Test.FAKECore
{
    using Machine.Specifications;

    using Microsoft.FSharp.Collections;

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
                .ShouldEqual(ChangeLogHelper.ChangeLogEntry.New("0.3.0", "0.3.0", new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2015, 12, 03)), FSharpList<ChangeLogHelper.Change>.Empty, false));

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
            new ChangeLogHelper.Change[] { ChangeLogHelper.Change.NewAdded("A") }.ToFSharpList(),
            false);

        private static readonly ChangeLogHelper.ChangeLogEntry expected2 = ChangeLogHelper.ChangeLogEntry.New(
            "1.0.0",
            "1.0.0",
            new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 12, 14)),
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
            ChangeLogHelper.ChangeLogEntry.New("2.0.0", "2.0.0-alpha2", new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 12, 24)), new ChangeLogHelper.Change[] { ChangeLogHelper.Change.NewAdded("B") }.ToFSharpList(), false);

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
            new ChangeLogHelper.Change[] { ChangeLogHelper.Change.NewChanged("Infer booleans for ints that only manifest 0 and 1.") }.ToFSharpList(),
            false);

        static readonly FSharpList<ChangeLogHelper.Change> expectedUnreleased = new ChangeLogHelper.Change[]
        {
            ChangeLogHelper.Change.NewAdded("Unreleased added"),
            ChangeLogHelper.Change.NewChanged("Unreleased changed"),
            ChangeLogHelper.Change.NewDeprecated("Unreleased deprecated"),
            ChangeLogHelper.Change.NewRemoved("Unreleased removed"),
            ChangeLogHelper.Change.NewFixed("Unreleased fixed"),
            ChangeLogHelper.Change.NewSecurity("Unreleased security")
        }.ToFSharpList();

        static readonly Microsoft.FSharp.Core.FSharpOption<string> expectedDescription = 
            new Microsoft.FSharp.Core.FSharpOption<string>("All notable changes to this project will be documented in this file.\n\nThe format is based on [Keep a Changelog](http://keepachangelog.com/)\nand this project adheres to [Semantic Versioning](http://semver.org/).");

        static ChangeLogHelper.ChangeLog _result;

        Because of = () =>  _result = ChangeLogHelper.parseChangeLog(Changes.FromString(validData));

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
}

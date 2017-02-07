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
            return data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
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
- Line 1 0.2.0-beta1 Fixed
- Line 2 0.2.0-beta1 Fixed

## 0.2.0-beta1 - 2015-10-06
### Changed
- Line 1 0.2.0-beta1 Changed
- Line 2 0.2.0-beta1 Changed

[Unreleased]: https://github.com/olivierlacan/keep-a-changelog/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/olivierlacan/keep-a-changelog/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/olivierlacan/keep-a-changelog/compare/v0.1.0...v0.2.0
What’s the point of a change log?";

        private static readonly ChangeLogHelper.ChangeLog expected = ChangeLogHelper.ChangeLog.New(
            "0.3.0",
            "0.3.0",
            new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2015, 12, 03)),
            new[]
            {
                "Added: Line 1 0.3.0 Added",
                "Added: Line 2 0.3.0 Added",
                "Fixed: Line 1 0.3.0 Fixed",
                "Fixed: Line 2 0.3.0 Fixed"
            }.ToFSharpList());

        It should_parse =
            () => ChangeLogHelper.parseChangeLog(Changes.FromString(validData)).ShouldEqual(expected);

        It should_parse_empty_changes =
            () => ChangeLogHelper.parseChangeLog(Changes.FromString(@"## [0.3.0] - 2015-12-03"))
                .ShouldEqual(ChangeLogHelper.ChangeLog.New("0.3.0", "0.3.0", new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2015, 12, 03)), FSharpList<string>.Empty));

        It should_throws_on_empty_seq_input =
            () => Catch.Exception(() => ChangeLogHelper.parseChangeLog(new string[] { }));

        It should_throws_on_null_input =
            () => Catch.Exception(() => ChangeLogHelper.parseChangeLog(null));

        It should_throws_on_single_empty_string_input =
            () => Catch.Exception(() => ChangeLogHelper.parseChangeLog(new[] { "" }));
    }

    public class when_parsing_many_alpha_versions_in_change_log
    {
        const string input = @"
# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/) 
and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]

### [2.0.0-alpha] - 2013-12-15
* A

#### 2.0.0-alpha2 - 2013-12-24
* B

#### 2.0.0-alpha001 - 2013-12-15
* A";

        static readonly ChangeLogHelper.ChangeLog expected =
            ChangeLogHelper.ChangeLog.New("2.0.0", "2.0.0-alpha2", new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 12, 24)), new[] { "B" }.ToFSharpList());

        It should_parse =
           () => ChangeLogHelper.parseChangeLog(Changes.FromString(input)).ShouldEqual(expected);
    }

    public class when_parsing_all_changes
    {
        const string validData = @" 

# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/) 
and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]

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


        static readonly ChangeLogHelper.ChangeLog expected = ChangeLogHelper.ChangeLog.New(
            "1.1.10",
            "1.1.10",
            new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 09, 12)),
            new[]
            {
                "Added: Support for heterogeneous XML attributes.",
                "Added: Make CsvFile re-entrant.",
                "Added: Support for compressed HTTP responses.",
                "Fixed: Fix JSON conversion of 0 and 1 to booleans.",
                "Fixed: Fix XmlProvider problems with nested elements and elements with same name in different namespaces."
            }.ToFSharpList());

        static readonly ChangeLogHelper.ChangeLog expected2 = ChangeLogHelper.ChangeLog.New(
            "1.1.9",
            "1.1.9",
            new Microsoft.FSharp.Core.FSharpOption<DateTime>(new DateTime(2013, 07, 21)),
            new[] { "Changed: Infer booleans for ints that only manifest 0 and 1." }.ToFSharpList());

        static FSharpList<ChangeLogHelper.ChangeLog> _result;

        Because of = () => _result = ChangeLogHelper.parseChanges(Changes.FromString(validData));

        It should_find_both_entries =
            () => _result.Length.ShouldEqual(2);

        It should_find_the_first_entry =
            () => _result.First().ShouldEqual(expected);

        It should_find_the_second_entry =
            () => _result.Skip(1).First().ShouldEqual(expected2);
    }
}

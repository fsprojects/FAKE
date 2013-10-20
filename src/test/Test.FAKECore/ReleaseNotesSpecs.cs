using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Collections;

namespace Test.FAKECore
{
    public class when_parsing_release_notes
    {
        const string data = @"
### New in 1.1.9 (Released 2013/07/21)
* Infer booleans for ints that only manifest 0 and 1.
* Support for partially overriding the Schema in CsvProvider.
* PreferOptionals and SafeMode parameters for CsvProvider.

### New in 1.1.10 (Released 2013/09/12)
* Support for heterogeneous XML attributes.
* Make CsvFile re-entrant. 
* Support for compressed HTTP responses. 
* Fix JSON conversion of 0 and 1 to booleans.
* Fix XmlProvider problems with nested elements and elements with same name in different namespaces.";

        static readonly ReleaseNotes expected = new ReleaseNotes("1.1.10", "1.1.10",
            new [] {"Support for heterogeneous XML attributes.", 
                    "Make CsvFile re-entrant.",
                    "Support for compressed HTTP responses.",
                    "Fix JSON conversion of 0 and 1 to booleans.",
                    "Fix XmlProvider problems with nested elements and elements with same name in different namespaces."}
            .ToFSharpList());

        It should_parse_it_properly =
            () => ReleaseNotesModule.parseReleaseNotes(data.Split(new [] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries))
                .ShouldEqual(expected);
    }
}
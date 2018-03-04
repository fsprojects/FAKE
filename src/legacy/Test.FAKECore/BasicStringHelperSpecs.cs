using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_using_basic_string_helper
    {
        It should_not_separate_empty_text =
            () => StringHelper.separated("test", new List<string>())
                      .ShouldBeEmpty();

        It should_separate_one_line =
            () => StringHelper.separated("test", new List<string> { "first" })
                      .ShouldEqual("first");

        It should_separate_three_lines =
            () => StringHelper.separated("-", new List<string> { "first", "second", "third" })
                      .ShouldEqual("first-second-third");

        It should_separate_three_lines_with_line_ends =
            () => StringHelper.toLines(new List<string> { "first", "second", "third" })
                      .ShouldEqual(string.Format("first{0}second{0}third", Environment.NewLine));

        It should_separate_two_lines_with_blank =
            () => StringHelper.separated(" ", new List<string> { "first", "second" })
                      .ShouldEqual("first second");
    }

    public class when_checking_strings
    {
        It should_detect_whitespace_string =
            () => StringHelper.isNullOrWhiteSpace("\n \r  ").ShouldBeTrue();

        It should_detect_non_whitespace_string =
            () => StringHelper.isNullOrWhiteSpace("\n \r s ").ShouldBeFalse();

        It should_detect_empty_string_as_whitespace_string =
            () => StringHelper.isNullOrWhiteSpace("").ShouldBeTrue();
    }
}
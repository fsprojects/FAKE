using System.Collections.Generic;
using System.Linq;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_using_basic_string_helper
    {
        It should_not_separate_empty_text =
            () => BasicStringHelper.separated("test", new List<string>())
                      .ShouldBeEmpty();

        It should_separate_one_line =
            () => BasicStringHelper.separated("test", new List<string> { "first" })
                      .ShouldEqual("first");

        It should_separate_three_lines =
            () => BasicStringHelper.separated("-", new List<string> { "first", "second", "third" })
                      .ShouldEqual("first-second-third");

        It should_separate_three_lines_with_line_ends =
            () => BasicStringHelper.toLines(new List<string> { "first", "second", "third" })
                      .ShouldEqual("first\r\nsecond\r\nthird");

        It should_separate_two_lines_with_blank =
            () => BasicStringHelper.separated(" ", new List<string> { "first", "second" })
                      .ShouldEqual("first second");
    }

    public class when_checking_strings
    {
        It should_detect_whitespace_string =
            () => BasicStringHelper.isNullOrWhiteSpace("\n \r  ").ShouldBeTrue();

        It should_detect_non_whitespace_string =
            () => BasicStringHelper.isNullOrWhiteSpace("\n \r s ").ShouldBeFalse();

        It should_detect_empty_string_as_whitespace_string =
            () => BasicStringHelper.isNullOrWhiteSpace("").ShouldBeTrue();
    }
}
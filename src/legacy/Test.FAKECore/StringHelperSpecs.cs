using System.Linq;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_normalizing_version
    {
        It should_not_remove_the_last_version_if_it_is_not_empty =
            () => StringHelper.NormalizeVersion("0.1.2.5").ShouldEqual("0.1.2.5");

        It should_remove_the_last_two_versions_if_they_are_empty =
            () => StringHelper.NormalizeVersion("0.1.0.0").ShouldEqual("0.1");

        It should_remove_the_last_version_if_it_is_empty =
            () => StringHelper.NormalizeVersion("0.1.2.0").ShouldEqual("0.1.2");

        It should_remove_the_fith_part =
            () => StringHelper.NormalizeVersion("0.1.2.1.6").ShouldEqual("0.1.2.1");

        It should_remove_the_sixth_part =
            () => StringHelper.NormalizeVersion("0.1.2.1.6.5").ShouldEqual("0.1.2.1");

        It should_remove_the_fith_part_and_trim =
            () => StringHelper.NormalizeVersion("0.1.2.0.6").ShouldEqual("0.1.2");

        It should_accept_single_versions_like_travis_build_numbers =
            () => StringHelper.NormalizeVersion("42").ShouldEqual("42");

    }

    public class when_using_string_helper
    {
        It should_read_the_test_file =
            () => StringHelper.ReadFile("TestData/AllObjects.txt")
                      .Count().ShouldEqual(3578);
    }

    public class when_converting_to_windows_line_endings
    {
        It should_convert_linux_text_to_windows_text =
            () => StringHelper.ConvertTextToWindowsLineBreaks("This is my text\nI love it\n\nreally.\n")
                      .ShouldEqual("This is my text\r\nI love it\r\n\r\nreally.\r\n");

        It should_convert_mac_text_to_windows_text =
            () => StringHelper.ConvertTextToWindowsLineBreaks("This is my text\rI love it\r\rreally.\r")
                      .ShouldEqual("This is my text\r\nI love it\r\n\r\nreally.\r\n");

        It should_convert_text_to_windows_text =
            () => StringHelper.ConvertTextToWindowsLineBreaks("This is my text\rI love it\n\r\nreally.\n")
                      .ShouldEqual("This is my text\r\nI love it\r\n\r\nreally.\r\n");

        It should_convert_window_text_to_windows_text =
            () => StringHelper.ConvertTextToWindowsLineBreaks("This is my text\r\nI love it\r\n\r\nreally.\r\n")
                      .ShouldEqual("This is my text\r\nI love it\r\n\r\nreally.\r\n");
    }
}

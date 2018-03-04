using System.Linq;
using Fake;
using Machine.Specifications;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore.Globbing.TestSample1
{
    public class when_extracting_zip : BaseFunctions
    {
        private const string TempDir = "temptest";

        Establish context = () =>
        {
            FileHelper.CleanDir(TempDir);
            ZipHelper.Unzip(TempDir, "Globbing/TestSample1/SampleApp.zip");
        };

        protected static string[] Files;
    }

    public class when_scanning_for_a_folder : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include("/**/SampleApp/").ToArray();

        It should_find_the_dir =
            () => Files[0].ShouldEndWith("SampleApp");

        It should_match_1_directory = () => Files.Length.ShouldEqual(1);
    }


    public class when_scanning_for_a_folder_with_asterisk : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include("/**/Sample*/").ToArray();

        It should_find_the_dir =
            () => Files[0].ShouldEndWith("SampleApp");

        It should_match_1_directory = () => Files.Length.ShouldEqual(1);
    }

    public class when_scanning_for_folders : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include("/temptest/**/").OrderBy(x => x).ToArray();

        It should_find_the_main_dir =
            () => Files[0].ShouldEndWith("temptest");

        It should_find_the_first_subdir =
            () => Files[1].ShouldEndWith("SampleApp");

        It should_find_the_second_subdir =
            () => Files[2].ShouldEndWith("bin");

        It should_find_all_files_in_the_subdir =
            () => Files[6].ShouldEndWith("test.rename");

        It should_match_9_entries = () => Files.Length.ShouldEqual(9);
    }

    public class when_scanning_in_zip : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include("temptest/SampleApp/bin/**/*.*").ToArray();

        It should_match_2_files = () => Files.Length.ShouldEqual(2);
    }

    public class when_scanning_for_concrete_dll_in_zip : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include("temptest/SampleApp/bin/SampleApp.dll").ToArray();

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
        It should_find_sample_app = () => Files.First().ShouldEndWith("SampleApp.dll");
    }

    public class when_scanning_for_any_dll_in_zip : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include("temptest/SampleApp/bin/*.dll").ToArray();

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
        It should_find_sample_app = () => Files.First().ShouldEndWith("SampleApp.dll");
    }

    public class when_scanning_for_any_dll_in_an_subfolder_in_zip : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include("temptest/**/*.dll").ToArray();

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
        It should_find_sample_app = () => Files.First().ShouldEndWith("SampleApp.dll");
    }

    public class when_scanning_for_concrete_dll_in_subfolder : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include("temptest/**/SampleApp.dll").ToArray();

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
        It should_find_sample_app = () => Files.First().ShouldEndWith("SampleApp.dll");
    }
}

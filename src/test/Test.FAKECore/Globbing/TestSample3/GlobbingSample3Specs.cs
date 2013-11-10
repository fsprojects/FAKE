using System.Linq;
using Fake;
using Machine.Specifications;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore.Globbing.TestSample3
{
    public class when_extracting_zip : BaseFunctions
    {
        const string TempDir = "temptest";

        protected static string[] Files;

        Establish context = () =>
        {
            FileHelper.CleanDir(TempDir);
            ZipHelper.Unzip(TempDir, "Globbing/TestSample3/Sample3.zip");
        };
    }

    public class when_scanning_for_txt_files : when_extracting_zip
    {
        Because of = () => Files = FileSystem.find("temptest/**/*.txt").ToArray();

        It should_find_names_txt = () => Files[0].ShouldEndWith("Names.txt");
        It should_match_1_file = () => Files.Length.ShouldEqual(1);
    }

    public class when_scanning_for_txt_files_in_any_subfolder : when_extracting_zip
    {
        static string _expectedFile =
            FileSystemHelper.FullName(".\\temptest\\Folder1\\Subfolder1\\SubFolder2\\TextFiles\\Names.txt");

        Because of = () => Files = FileSystem.find("./**/*.txt").ToArray();

        It should_find_names_txt = () => Files.ShouldContain(_expectedFile);
    }

    public class when_scanning_for_txt_files_using_backslash : when_extracting_zip
    {
        Because of = () => Files = FileSystem.find("temptest\\**\\*.txt").ToArray();

        It should_find_names_txt = () => Files[0].ShouldEndWith("Names.txt");
        It should_match_1_file = () => Files.Length.ShouldEqual(1);
    }

    public class when_scanning_for_text_files : when_extracting_zip
    {
        Because of = () => Files = FileSystem.find("temptest/**/*.text").ToArray();

        It should_find__names_txt_text =
            () => Files[1].ShouldEndWith("Folder1\\Subfolder1\\SubFolder2\\TextFiles\\Names.txt.text");

        It should_find_names_text =
            () => Files[0].ShouldEndWith("Folder1\\Subfolder1\\SubFolder2\\TextFiles\\Names.text");

        It should_match_2_files = () => Files.Length.ShouldEqual(2);
    }

    public class when_scanning_for_txt_files_with_a_special_subfolder_which_doesnt_exist : when_extracting_zip
    {
        Because of = () => Files = FileSystem.find("temptest/**/SubFolderWhichDoesntExist/**/*.txt").ToArray();

        It should_not_match_a_file = () => Files.Length.ShouldEqual(0);
    }

    public class when_scanning_for_txt_files_with_a_special_subfolder : when_extracting_zip
    {
        Because of = () => Files = FileSystem.find("temptest/**/SubFolder2/**/*.txt").ToArray();

        It should_find_names_txt = () => Files[0].ShouldEndWith("Names.txt");
        It should_match_1_file = () => Files.Length.ShouldEqual(1);
    }
}
using System.Linq;
using Fake;
using Machine.Specifications;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore.Globbing.TestSample3
{
    public class when_extracting_zip : BaseFunctions
    {
        private const string TempDir = "temptest";

        Establish context = () =>
        {
            FileHelper.CleanDir(TempDir);
            ZipHelper.Unzip(TempDir, "Globbing/TestSample3/Sample3.zip");
        };

        protected static string[] Files;
    }

    public class when_scanning_for_txt_files : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Search.find("temptest/**/*.txt").ToArray();

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
        It should_find_names_txt = () => Files[0].ShouldEndWith("Names.txt");
    }

    public class when_scanning_for_text_files : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Search.find("temptest/**/*.text").ToArray();

        It should_match_2_files = () => Files.Length.ShouldEqual(2);
        It should_find_names_text = () => Files[0].ShouldEndWith("Names.text");
        It should_find__names_txt_text = () => Files[1].ShouldEndWith("Names.txt.text");
    }

    public class when_scanning_for_txt_files_with_a_special_subfolder_which_doesnt_exist : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Search.find("temptest/**/SubFolderWhichDoesntExist/**/*.txt").ToArray();

        It should_not_match_a_file = () => Files.Length.ShouldEqual(0);
    }

    public class when_scanning_for_txt_files_with_a_special_subfolder : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Search.find("temptest/**/SubFolder2/**/*.txt").ToArray();

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
        It should_find_names_txt = () => Files[0].ShouldEndWith("Names.txt");
    }
}
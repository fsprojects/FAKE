using System.IO;
using System.Linq;
using Fake;
using Machine.Specifications;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore.Globbing.TestSample4
{
    public class when_extracting_zip : BaseFunctions
    {
        const string TempDir = "temptest";

        protected static string[] Files;

        Establish context = () =>
        {
            FileHelper.CleanDir(TempDir);
            ZipHelper.Unzip(TempDir, "Globbing/TestSample4/Sample4.zip");
        };
    }

    public class when_scanning_with_asterisk_in_the_middle_and_dot : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include("temptest/**/Specs1.*.testending").ToArray();

        It should_find_the_file =
            () =>
                Files[0].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}SubFolder2{0}TextFiles{0}Specs1.Awesome.testending", Path.DirectorySeparatorChar));
        
        It should_match_1_file = () => Files.Length.ShouldEqual(1);
    }

    public class when_scanning_with_asterisk_in_the_middle : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include("temptest/**/Specs*.testending").ToArray();

        It should_find_the_first_file =
            () => Files[0].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}Specs2.Awesome.testending", Path.DirectorySeparatorChar));

        It should_find_the_second_file =
            () => Files[1].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}SubFolder2{0}TextFiles{0}Specs1.Awesome.testending", Path.DirectorySeparatorChar));

        It should_match_2_files = () => Files.Length.ShouldEqual(2);
    }

    public class when_scanning_with_two_asterisks_in_the_middle : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include("temptest/**/Specs*.*.testending").ToArray();

        It should_find_the_first_file =
            () => Files[0].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}Specs2.Awesome.testending", Path.DirectorySeparatorChar));

        It should_find_the_second_file =
            () => Files[1].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}SubFolder2{0}TextFiles{0}Specs1.Awesome.testending", Path.DirectorySeparatorChar));

        It should_match_2_files = () => Files.Length.ShouldEqual(2);
    }

    public class when_scanning_with_two_asterisks_and_backslashes_in_the_middle : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include("temptest\\**\\Specs*.*.testending").ToArray();

        It should_find_the_first_file =
            () => Files[0].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}Specs2.Awesome.testending", Path.DirectorySeparatorChar));

        It should_find_the_second_file =
            () => Files[1].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}SubFolder2{0}TextFiles{0}Specs1.Awesome.testending", Path.DirectorySeparatorChar));

        It should_match_2_files = () => Files.Length.ShouldEqual(2);
    }
}

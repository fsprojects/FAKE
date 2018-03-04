using System.IO;
using System.Linq;
using Fake;
using Machine.Specifications;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore.Globbing.TestSample5
{
    public class when_extracting_zip : BaseFunctions
    {
        protected static readonly string TempDir = FileSystemHelper.FullName("temptest");

        protected static string[] Files;

        Establish context = () =>
        {
            FileHelper.CleanDir(TempDir);
            ZipHelper.Unzip(TempDir, "Globbing/TestSample5/Sample5.zip");
        };

        public static string FullPath(string pattern)
        {
            return TempDir + pattern;
        }
    }

    public class when_scanning_with_asterisk_in_the_middle_and_dot : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include(FullPath("/**/Specs1.*.testending")).ToArray();

        It should_find_the_file =
            () => Files[0].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}SubFolder2{0}TextFiles{0}Specs1.Awesome.testending", Path.DirectorySeparatorChar));

        It should_find_the_file_with_absolute_path =
            () => Files[0].ShouldStartWith(TempDir);
        
        It should_match_1_file = () => Files.Length.ShouldEqual(1);
    }

    public class when_scanning_with_asterisk_in_the_middle : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include(FullPath("/**/Specs*.testending")).ToArray();

        It should_find_the_first_file =
            () => Files[0].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}Specs2.Awesome.testending", Path.DirectorySeparatorChar));

        It should_find_the_file_with_absolute_path =
            () => Files[0].ShouldStartWith(TempDir);

        It should_find_the_second_file =
            () => Files[1].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}SubFolder2{0}TextFiles{0}Specs1.Awesome.testending", Path.DirectorySeparatorChar));

        It should_find_the_second_file_with_absolute_path =
            () => Files[1].ShouldStartWith(TempDir);

        It should_match_2_files = () => Files.Length.ShouldEqual(2);
    }

    public class when_scanning_with_two_asterisks_in_the_middle : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include(FullPath(Pattern)).ToArray();

        It should_set_the_base_directory =
            () => FileSystem.Include(FullPath(Pattern))
                   .BaseDirectory.ShouldEqual(Directory.GetCurrentDirectory());

        It should_set_the_pattern =
            () => FileSystem.Include(FullPath(Pattern))
                   .Includes.First().ShouldEqual(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "temptest" + Pattern);

        It should_create_the_full_path =
            () => FullPath(Pattern)
                   .ShouldStartWith(Directory.GetCurrentDirectory());

        It should_find_the_first_file =
            () => Files[0].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}Specs2.Awesome.testending", Path.DirectorySeparatorChar));

        It should_find_the_second_file =
            () => Files[1].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}SubFolder2{0}TextFiles{0}Specs1.Awesome.testending", Path.DirectorySeparatorChar));

        It should_match_2_files = () => Files.Length.ShouldEqual(2);
        const string Pattern = "/**/Specs*.*.testending";
    }

    public class when_scanning_with_two_asterisks_and_backslashes_in_the_middle : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include(FullPath("\\**\\Specs*.*.testending")).ToArray();

        It should_find_the_first_file =
            () => Files[0].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}Specs2.Awesome.testending", Path.DirectorySeparatorChar));

        It should_find_the_file_with_absolute_path =
            () => Files[0].ShouldStartWith(TempDir);

        It should_find_the_second_file =
            () => Files[1].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}SubFolder2{0}TextFiles{0}Specs1.Awesome.testending", Path.DirectorySeparatorChar));

        It should_find_the_second_file_with_absolute_path =
            () => Files[1].ShouldStartWith(TempDir);

        It should_match_2_files = () => Files.Length.ShouldEqual(2);
    }

    public class when_scanning_with_folder_change : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include(FullPath("\\Folder1\\..\\Folder1\\Subfolder1\\Specs*.*.testending")).ToArray();

        It should_find_the_first_file =
            () => Files[0].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}Specs2.Awesome.testending", Path.DirectorySeparatorChar));

        It should_find_the_file_with_absolute_path =
            () => Files[0].ShouldStartWith(TempDir);

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
    }

    public class when_scanning_with_invalid_folder_change : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include(FullPath("\\Folder1\\..\\..\\Folder1\\Subfolder1\\Specs*.*.testending")).ToArray();

        It should_match_a_file = () => Files.Length.ShouldEqual(0);
    }

    public class when_scanning_with_multiple_folder_change : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include(FullPath("\\Folder1\\..\\..\\temptest\\Folder1\\Subfolder1\\Specs*.*.testending")).ToArray();

        It should_find_the_first_file =
            () => Files[0].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}Specs2.Awesome.testending", Path.DirectorySeparatorChar));

        It should_find_the_file_with_absolute_path =
            () => Files[0].ShouldStartWith(TempDir);

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
    }
}

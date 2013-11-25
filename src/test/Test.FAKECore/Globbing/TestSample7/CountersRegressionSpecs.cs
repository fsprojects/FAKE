using System.IO;
using System.Linq;
using Fake;
using Machine.Specifications;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore.Globbing.TestSample7
{
    public class when_extracting_zip : BaseFunctions
    {
        protected static readonly string TempDir = FileSystemHelper.FullName("temptest");

        protected static string[] Files;

        Establish context = () =>
        {
            FileHelper.CleanDir(TempDir);
            ZipHelper.Unzip(TempDir, "Globbing/TestSample7/Sample7.zip");
        };

        public static string FullPath(string pattern)
        {
            return TempDir + pattern;
        }
    }

    public class when_scanning_for_all_files : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include(FullPath("/counters/*.*")).ToArray();

        It should_find_the_first_file =
            () => Files[0].ShouldEndWith("temptest\\counters\\COUNTERS.mdf");

        It should_find_the_second_file =
            () => Files[1].ShouldEndWith("temptest\\counters\\COUNTERS_log.ldf");

        It should_find_the_file_with_absolute_path =
            () => Files[0].ShouldStartWith(TempDir);
        
        It should_match_2_files = () => Files.Length.ShouldEqual(2);
    }

    public class when_scanning_for_all_files_using_backslashes : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include(FullPath("\\counters\\*.*").Replace("/","\\")).ToArray();

        It should_find_the_first_file =
            () => Files[0].ShouldEndWith("temptest\\counters\\COUNTERS.mdf");

        It should_find_the_second_file =
            () => Files[1].ShouldEndWith("temptest\\counters\\COUNTERS_log.ldf");

        It should_find_the_file_with_absolute_path =
            () => Files[0].ShouldStartWith(TempDir);

        It should_match_2_files = () => Files.Length.ShouldEqual(2);
    }
}

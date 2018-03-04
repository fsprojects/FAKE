using System.IO;
using System.Linq;
using Fake;
using Machine.Specifications;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore.Globbing.TestSample6
{
    public class when_extracting_zip : BaseFunctions
    {
        protected static readonly string TempDir = FileSystemHelper.FullName("temptest");

        protected static string[] Files;

        Establish context = () =>
        {
            FileHelper.CleanDir(TempDir);
            ZipHelper.Unzip(TempDir, "Globbing/TestSample6/Sample6.zip");
        };

        public static string FullPath(string pattern)
        {
            return TempDir + pattern;
        }
    }

    public class when_scanning_for_all_files : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include(FullPath("/curl/*.*")).ToArray();

        It should_find_the_file =
            () => Files[0].ShouldEndWith(string.Format("temptest{0}curl{0}curl.exe", Path.DirectorySeparatorChar));

        It should_find_the_file_with_absolute_path =
            () => Files[0].ShouldStartWith(TempDir);
        
        It should_match_1_file = () => Files.Length.ShouldEqual(1);
    }
}

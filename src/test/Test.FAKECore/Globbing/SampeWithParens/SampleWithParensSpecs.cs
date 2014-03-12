using System.Linq;
using Fake;
using Machine.Specifications;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore.Globbing.SampleWithParens
{
    public class when_extracting_zip : BaseFunctions
    {
        protected static readonly string TempDir = FileSystemHelper.FullName("temptest");

        protected static string[] Files;

        Establish context = () =>
        {
            FileHelper.CleanDir(TempDir);
            ZipHelper.Unzip(TempDir, "Globbing/SampeWithParens/SampleWithParens.zip");
        };

        public static string FullPath(string pattern)
        {
            return TempDir + pattern;
        }
    }

    public class when_scanning_for_all_files : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include(FullPath("/**/*.*proj")).ToArray();

        It should_find_the_first_file = () => Files[0].ShouldEndWith("a(NET40).csproj");

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
    }

    public class when_scanning_for_parens : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include(FullPath("/**/*(NET40).*proj")).ToArray();

        It should_find_the_first_file = () => Files[0].ShouldEndWith("a(NET40).csproj");

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
    }
}
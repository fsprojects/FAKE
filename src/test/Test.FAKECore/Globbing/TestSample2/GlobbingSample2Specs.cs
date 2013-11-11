using System.Linq;
using Fake;
using Machine.Specifications;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore.Globbing.TestSample2
{
    public class when_extracting_sample_zip : BaseFunctions
    {
        const string TempDir = "temptest";

        Establish context = () =>
        {
            FileHelper.CleanDir(TempDir);
            ZipHelper.Unzip(TempDir, "Globbing/TestSample2/TFSSample.zip");
        };
    }

    public class when_scanning_in_zip_with_base_dir : when_extracting_sample_zip
    {
        static string[] Files;

        Because of = () => Files = FileSystem.Include("./**/packages.config").ToArray();

        It should_match_2_files = () => Files.Length.ShouldEqual(2);
    }
}

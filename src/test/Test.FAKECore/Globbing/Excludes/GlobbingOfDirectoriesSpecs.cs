using System.Linq;
using Fake;
using Machine.Specifications;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore.Globbing.Excludes
{
    public class when_extracting_sample_zip : BaseFunctions
    {
        const string TempDir = "temptest";

        Establish context = () =>
        {
            FileHelper.CleanDir(TempDir);
            ZipHelper.Unzip(TempDir, "Globbing/Excludes/TFSSample.zip");
        };
    }

    public class when_scanning_in_zip_with_base_dir : when_extracting_sample_zip
    {
        static string[] Files;

        Because of = () =>
        {
            var includes = FileSetHelper.Include("./**/packages.config");
            Files = FileSetHelper.ScanImmediately(includes).ToArray();
        };

        It should_match_2_files = () => Files.Length.ShouldEqual(2);
    }
}
using System.IO;
using System.Linq;
using Fake;
using Machine.Specifications;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore.Globbing.Directories
{
    public class when_extracting_zip : BaseFunctions
    {
        private const string TempDir = "temptest";

        protected static string[] Globbing(string pattern, string baseDir)
        {
            return FileSystem.Search.find(pattern).ToArray();
        }

        Establish context = () =>
        {
            FileHelper.CleanDir(TempDir);
            ZipHelper.Unzip(TempDir, "Globbing/Directories/SampleApp.zip");
        };

        protected static string[] Files;
    }

    public class when_scanning_in_zip : when_extracting_zip
    {
        private Because of = () => Files = Globbing("temptest/SampleApp/bin/**/*.*", "");

        It should_match_3_files = () => Files.Length.ShouldEqual(3);
        It should_find_ilmerge = () => Files[0].ShouldEndWith("ilmerge.exclude");
        It should_find_sample_app = () => Files[1].ShouldEndWith("SampleApp.dll");
    }

    public class when_scanning_for_concrete_dll_in_zip : when_extracting_zip
    {
        private Because of = () => Files = Globbing("temptest/SampleApp/bin/SampleApp.dll", "");

        It should_match_3_files = () => Files.Length.ShouldEqual(1);
        It should_find_sample_app = () => Files.First().ShouldEndWith("SampleApp.dll");
    }

    public class when_scanning_for_concrete_dll_in_subfolder : when_extracting_zip
    {
        private Because of = () => Files = Globbing("temptest/**/SampleApp.dll", "");

        It should_match_3_files = () => Files.Length.ShouldEqual(1);
        It should_find_sample_app = () => Files.First().ShouldEndWith("SampleApp.dll");
    }

    public class when_scanning_in_zip_with_base_dir : when_extracting_zip
    {
        private Because of = () => Files = Globbing("*", "SampleApp/bin");

        It should_match_3_files = () => Files.Length.ShouldEqual(3);
        It should_find_ilmerge = () => Files[0].ShouldEndWith("ilmerge.exclude");
        It should_find_sample_app = () => Files[1].ShouldEndWith("SampleApp.dll");
    }
}
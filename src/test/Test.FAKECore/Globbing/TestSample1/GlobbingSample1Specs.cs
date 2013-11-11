using System.Linq;
using Fake;
using Machine.Specifications;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore.Globbing.TestSample1
{
    public class when_extracting_zip : BaseFunctions
    {
        private const string TempDir = "temptest";

        Establish context = () =>
        {
            FileHelper.CleanDir(TempDir);
            ZipHelper.Unzip(TempDir, "Globbing/TestSample1/SampleApp.zip");
        };

        protected static string[] Files;
    }

    public class when_scanning_in_zip : when_extracting_zip
    {
        Because of = () => Files = FileSystem.find("temptest/SampleApp/bin/**/*.*").ToArray();

        It should_match_2_files = () => Files.Length.ShouldEqual(2);
        It should_find_ilmerge = () => Files[0].ShouldEndWith("ilmerge.exclude");
        It should_find_sample_app = () => Files[1].ShouldEndWith("SampleApp.dll");
    }

    public class when_scanning_for_concrete_dll_in_zip : when_extracting_zip
    {
        Because of = () => Files = FileSystem.find("temptest/SampleApp/bin/SampleApp.dll").ToArray();

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
        It should_find_sample_app = () => Files.First().ShouldEndWith("SampleApp.dll");
    }

    public class when_scanning_for_any_dll_in_zip : when_extracting_zip
    {
        Because of = () => Files = FileSystem.find("temptest/SampleApp/bin/*.dll").ToArray();

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
        It should_find_sample_app = () => Files.First().ShouldEndWith("SampleApp.dll");
    }

    public class when_scanning_for_any_dll_in_an_subfolder_in_zip : when_extracting_zip
    {
        Because of = () => Files = FileSystem.find("temptest/**/*.dll").ToArray();

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
        It should_find_sample_app = () => Files.First().ShouldEndWith("SampleApp.dll");
    }

    public class when_scanning_for_concrete_dll_in_subfolder : when_extracting_zip
    {
        Because of = () => Files = FileSystem.find("temptest/**/SampleApp.dll").ToArray();

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
        It should_find_sample_app = () => Files.First().ShouldEndWith("SampleApp.dll");
    }
}

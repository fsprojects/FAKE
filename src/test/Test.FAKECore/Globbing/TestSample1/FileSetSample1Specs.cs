using System.IO;
using System.Linq;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.Globbing.TestSample1
{
    public class when_enumerating_a_file_set : when_extracting_zip
    {
        static FileSystem.FileIncludes _files;
        Because of = () => _files = FileSystem.Include("temptest/**/*.*");

        It should_set_the_base_directory =
            () => _files.BaseDirectory.ShouldEqual(System.IO.Directory.GetCurrentDirectory());

        It should_find_first_file = () => _files.First().ShouldEndWith("SampleApp.csproj");
        It should_find_second_file = () => _files.Skip(1).First().ShouldEndWith("SampleApp.sln");
        It should_match_5_files = () => _files.Count().ShouldEqual(5);
    }

    public class when_enumerating_a_file_set_with_subfolder_pattern : when_extracting_zip
    {
        static FileSystem.FileIncludes _files;
        Because of = () => _files = FileSystem.Include("temptest/SampleApp/**/*.*");

        It should_set_the_base_directory =
            () => _files.BaseDirectory.ShouldEqual(System.IO.Directory.GetCurrentDirectory());

        It should_match_5_files = () => _files.Count().ShouldEqual(5);
    }

    public class when_enumerating_a_file_set_with_flat_pattern : when_extracting_zip
    {
        static FileSystem.FileIncludes _files;
        Because of = () => _files = FileSystem.Include("temptest/SampleApp/*.*");

        It should_set_the_base_directory =
            () => _files.BaseDirectory.ShouldEqual(System.IO.Directory.GetCurrentDirectory());

        It should_match_2_files = () => _files.Count().ShouldEqual(2);
    }

    public class when_enumerating_a_file_set_with_deep_subfolder_pattern : when_extracting_zip
    {
        static FileSystem.FileIncludes _files;
        Because of = () => _files = FileSystem.Include("temptest/SampleApp/bin/**/*.*");

        It should_set_the_base_directory = 
            () => _files.BaseDirectory.ShouldEqual(System.IO.Directory.GetCurrentDirectory());

        It should_find_ilmerge = () => _files.OrderBy(x => x).First().ShouldEndWith("ilmerge.exclude");
        It should_find_sample_app = () => _files.OrderBy(x => x).Skip(1).First().ShouldEndWith("SampleApp.dll");
        It should_match_2_files = () => _files.Count().ShouldEqual(2);
    }

    public class when_excluding_a_file : when_extracting_zip
    {
        Because of = () => Files =
            FileSystem
                .Include("temptest/SampleApp/bin/**/*.*")
                .ButNot("temptest/SampleApp/bin/**/ilmerge.exclude")
                .ToArray();

        It should_find_sample_app = () => Files.First().ShouldEndWith("SampleApp.dll");
        It should_match_1_file = () => Files.Count().ShouldEqual(1);
    }

    public class when_using_empty_pattern : when_extracting_zip
    {
        Because of = () => Files =
            FileSystem
                .Include("")
                .ToArray();

        It should_find_the_base_dir = () => Files.First().ShouldEqual(System.IO.Directory.GetCurrentDirectory());
        It should_match_no_files = () => Files.Length.ShouldEqual(1);
    }

    public class when_using_mupltiple_includes : when_extracting_zip
    {
        Because of = () => Files =
            FileSystem
                .Include("temptest/SampleApp/bin/SampleApp.dll")
                .And("temptest/SampleApp/bin/ilmerge.exclude")
                .ToArray();

        It should_find_ilmerge = () => Files.Skip(1).First().ShouldEndWith("ilmerge.exclude");
        It should_find_sample_app = () => Files.First().ShouldEndWith("SampleApp.dll");
        It should_match_2_files = () => Files.Length.ShouldEqual(2);
    }

    public class when_using_overlapping_includes : when_extracting_zip
    {
        Because of = () => Files =
            FileSystem
                .Include("temptest/SampleApp/bin/SampleApp.dll")
                .And("temptest/SampleApp/bin/*.*")
                .ToArray();

        It should_find_ilmerge = () => Files.Skip(1).First().ShouldEndWith("ilmerge.exclude");
        It should_find_sample_app = () => Files.First().ShouldEndWith("SampleApp.dll");
        It should_match_2_files = () => Files.Length.ShouldEqual(2);
    }

    public class when_excluding_all_files : when_extracting_zip
    {
        Because of = () => Files =
            FileSystem
                .Include("temptest/SampleApp/bin/**/*.*")
                .ButNot("temptest/SampleApp/bin/**/*.*")
                .ToArray();

        It should_match_no_file = () => Files.Count().ShouldEqual(0);
    }

    public class when_scanning_for_any_dll_in_a_special_basefolder : when_extracting_zip
    {
        Because of = () => Files =
            FileSystem
                .Include("**/*.dll")
                .SetBaseDirectory("temptest/")
                .ToArray();

        It should_find_sample_app = () => Files.First().ShouldEndWith(string.Format("temptest{0}SampleApp{0}bin{0}SampleApp.dll",Path.DirectorySeparatorChar));
        It should_match_1_file = () => Files.Length.ShouldEqual(1);
    }

    public class when_scanning_for_any_dll_and_starting_in_a_wrong_subfolder : when_extracting_zip
    {
        Because of = () => Files =
            FileSystem
                .Include("**/*.dll")
                .SetBaseDirectory("temptest/SampleApp/obj")
                .ToArray();

        It should_not_find_a_file = () => Files.Length.ShouldEqual(0);
    }

    public class when_scanning_for_any_dll_and_starting_in_a_subfolder_and_going_up : when_extracting_zip
    {
        Because of = () => Files =
            FileSystem
                .Include("../**/*.dll")
                .SetBaseDirectory("temptest/SampleApp/obj")
                .ToArray();

        It should_find_sample_app = () => Files.First().ShouldEndWith("SampleApp.dll");
        It should_match_1_file = () => Files.Length.ShouldEqual(1);
    }

    public class when_scanning_folder_with_trialing_slash : when_extracting_zip
    {
        Because of = () => Files =
            FileSystem
                .Include("temptest/SampleApp/obj/")
                .ToArray();

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
    }

    public class when_scanning_folder_without_trialing_slash : when_extracting_zip
    {
        Because of = () => Files =
            FileSystem
                .Include("temptest/SampleApp/obj")
                .ToArray();

        It should_match_1_file = () => Files.Length.ShouldEqual(1);
    }
}
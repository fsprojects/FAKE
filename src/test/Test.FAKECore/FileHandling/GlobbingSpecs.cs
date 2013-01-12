using System.IO;
using System.Linq;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.FileHandling
{
    public class when_creating_some_directories_and_scanning_them : BaseFunctions
    {
        Establish context = CreateTestDirStructure;

        It should_find_Dir2 = () => ScanCount("**/Dir2").ShouldEqual(1);
        It should_find_Sub1 = () => ScanCount("**/Sub1").ShouldEqual(6);
        It should_find_Sub1_in_sub2 = () => ScanCount("**/Sub2/Sub1").ShouldEqual(1);
        It should_find_Sub1_in_subfolder = () => ScanCount("**/**/Sub1").ShouldEqual(6);
        It should_find_Sub2 = () => ScanCount("**/Sub2").ShouldEqual(2);
        It should_find_only_the_test_dir = () => Scan("Test").Count.ShouldEqual(1);
        It should_find_test_directory_in_base_directory = () => Scan(@"Test").First().ShouldEqual(TestData.TestDir);
        It should_find_the_test_dir = () => Scan("Test").First().ShouldEqual(TestData.TestDir);
    }

    public class when_creating_some_files_and_scanning_them : BaseFunctions
    {
        Establish context = CreateTestFileStructure;
        It should_find_all_files_in_test_folder = () => ScanCount("Test/*.*").ShouldEqual(5);
        It should_find_every_nav_file_in_every_folder = () => ScanCount("**/*.nav").ShouldEqual(7);

        It should_find_file1 = () => ScanCount("**/file1.nav").ShouldEqual(3);
        It should_find_file1_with_every_extension = () => ScanCount("**/file1.*").ShouldEqual(5);
        It should_find_file_x = () => ScanCount("**/file?.n??").ShouldEqual(11);
        It should_find_file_x_in_subfolder = () => ScanCount("**/Sub1/**/file*.*").ShouldEqual(6);
        It should_not_find_a_nav_file_in_root = () => ScanCount("*.nav").ShouldEqual(0);
    }

    public class when_creating_some_files_and_scanning_them_with_concrete_path : BaseFunctions
    {
        Establish context = CreateTestFileStructure;
        It should_find_all_files_in_test_folder = () => ScanCount(FileSetHelper.DefaultBaseDir + "/Test/*.*").ShouldEqual(5);
        It should_find_every_nav_file_in_every_folder = () => ScanCount(FileSetHelper.DefaultBaseDir + "/Test/**/*.nav").ShouldEqual(7);

        It should_find_file1 = () => ScanCount(FileSetHelper.DefaultBaseDir + "/**/file1.nav").ShouldEqual(3);
        It should_find_file1_with_every_extension = () => ScanCount(FileSetHelper.DefaultBaseDir + "/**/file1.*").ShouldEqual(5);
        It should_find_file_x = () => ScanCount(FileSetHelper.DefaultBaseDir + "/**/file?.n??").ShouldEqual(11);
        It should_find_file_x_in_subfolder = () => ScanCount(FileSetHelper.DefaultBaseDir + "/Test/**/Sub1/**/file*.*").ShouldEqual(6);
        It should_not_find_a_nav_file_in_root = () => ScanCount(FileSetHelper.DefaultBaseDir + "/*.nav").ShouldEqual(0);
    }

    public class when_creating_some_files_and_scanning_by_convenion : BaseFunctions
    {
        Establish context = CreateTestFileStructure;

        It should_find_any_file =
            () => FileSystemHelper.FindFirstMatchingFile("*.*", TestData.TestDir)
                      .ShouldStartWith(TestData.TestDir);


        It should_find_fff_file =
            () => FileSystemHelper.FindFirstMatchingFile("*.fff", TestData.TestDir)
                      .ShouldEndWith("file2.fff");

        It should_not_find_fsx_file =
            () => Catch.Exception(() => FileSystemHelper.FindFirstMatchingFile("*.fsx", TestData.TestDir))
                      .ShouldBeOfType<FileNotFoundException>();
    }
}
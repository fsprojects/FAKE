using System.IO;
using Machine.Specifications;

namespace Test.FAKECore.FileHandling
{
    public class when_cleaning_directory_after_creating_test_structure : BaseFunctions
    {
        Establish context = CreateTestFileStructure;

        Because of = () => CleanDir(TestData.TestDir);

        It should_be_writeable =
            () => new DirectoryInfo(TestData.TestDir).Attributes.ShouldEqual(FileAttributes.Directory);

        It should_cleaned_all_dirs = () => AllDirectories().ShouldBeEmpty();
        It should_cleaned_all_files = () => AllFiles().ShouldBeEmpty();
        It should_still_exist = () => new DirectoryInfo(TestData.TestDir).Exists.ShouldBeTrue();
    }
}
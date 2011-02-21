using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.FileHandling
{
    public class when_copying_directory_with_subfolders_but_without_filter : BaseFunctions
    {
        Establish context = CreateTestFileStructure;

        Because of = () => FileHelper.CopyDir(Target, TestData.SubDir7, AllFilesFunction);

        It should_have_copied_all_files = 
            () => Directory.GetFiles(Target, "*.*", SearchOption.AllDirectories).Length.ShouldEqual(6);

        It should_have_copied_the_subfolder =
            () => Directory.GetDirectories(Target, "*", SearchOption.AllDirectories).Length.ShouldEqual(1);

        private static readonly string Target = string.Format("{0}\\CopyTo", TestData.TestDir);
    }

    public class when_copying_directory_but_without_filter : BaseFunctions
    {
        Establish context = CreateTestFileStructure;

        Because of = () => FileHelper.CopyDir(Target, TestData.SubDir1, AllFilesFunction);

        It should_have_copied_all_files =
            () => Directory.GetFiles(Target, "*.*", SearchOption.AllDirectories).Length.ShouldEqual(3);

        It should_not_have_copied_a_subfolder =
            () => Directory.GetDirectories(Target, "*", SearchOption.AllDirectories).ShouldBeEmpty();

        private static readonly string Target = string.Format("{0}\\CopyTo", TestData.TestDir);
    }
}
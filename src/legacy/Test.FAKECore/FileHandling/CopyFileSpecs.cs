using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.FileHandling
{
    public class when_copying_file_with_subfolders : BaseFunctions
    {
        private static readonly string DestinationDir = Path.Combine(TestData.TestDir, "destination");

        Establish context = CreateTestFileStructure;
        Because of = () => FileHelper.CopyFileWithSubfolder(
            TestData.TestDir + "/Dir6",
            DestinationDir,
            Path.Combine(TestData.TestDir, "Dir6/Sub1/file2.nat"));
        It should_find_file_in_destination_subfolder = () =>
            File.Exists(DestinationDir + "/Sub1/file2.nat").ShouldBeTrue();
    }

     public class when_copying_group_of_files_with_subfolders : BaseFunctions
     {
         private static readonly string DestinationDir = Path.Combine(TestData.TestDir, "destination");

         Establish context = CreateTestFileStructure;
         Because of = () => FileHelper.CopyWithSubfoldersTo(
            DestinationDir,
            new []
            {
                FileSystem.SetBaseDir(TestData.TestDir + "/Dir7", FileSystem.Include("**/*.nat"))
            });
         It should_find_file_in_destination_folder = () =>
             File.Exists(DestinationDir + "/file2.nat").ShouldBeTrue();
         It should_find_file_in_destination_subfolder = () =>
            File.Exists(DestinationDir + "/Sub1/file2.nat").ShouldBeTrue();
     }
}

using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.FileHandling
{
    public class when_detecting_subfolders
    {
        It should_detect_same_dir = () => FileSystemHelper.isSubfolderOf(new DirectoryInfo("C:/sub"), new DirectoryInfo("C:/sub")).ShouldBeTrue();
        It should_detect_same_dir_slashes = () => FileSystemHelper.isSubfolderOf(new DirectoryInfo("C:/sub/"), new DirectoryInfo("C:/sub")).ShouldBeTrue();
        It should_detect_1_level = () => FileSystemHelper.isSubfolderOf(new DirectoryInfo("C:/sub"), new DirectoryInfo("C:/sub/sub1")).ShouldBeTrue();
        It should_detect_2_levels = () => FileSystemHelper.isSubfolderOf(new DirectoryInfo("C:/sub"), new DirectoryInfo("C:/sub/sub1/sub2")).ShouldBeTrue();
        It should_detect_if_not_sub = () => FileSystemHelper.isSubfolderOf(new DirectoryInfo("C:/sub"), new DirectoryInfo("C:/main/sub/sub2")).ShouldBeFalse();
    }

    public class when_detecting_files_in_folders
    {
        It should_detect_same_dir = () => FileSystemHelper.isInFolder(new DirectoryInfo("C:/sub"), new FileInfo("C:/sub/file.txt")).ShouldBeTrue();
        It should_detect_same_dir_slashes = () => FileSystemHelper.isInFolder(new DirectoryInfo("C:/sub/"), new FileInfo("C:/sub/file.txt")).ShouldBeTrue();
        It should_detect_1_level = () => FileSystemHelper.isInFolder(new DirectoryInfo("C:/sub"), new FileInfo("C:/sub/sub1/file.txt")).ShouldBeTrue();
        It should_detect_2_levels = () => FileSystemHelper.isInFolder(new DirectoryInfo("C:/sub"), new FileInfo("C:/sub/sub1/sub2/file.txt")).ShouldBeTrue();
        It should_detect_caseinsensitive = () =>
            FileSystemHelper.isInFolder(new DirectoryInfo("C:/code/uen/data"), new FileInfo("C:/code/uen/Data/Demo/Prozessvorlagen/4000004.XML")).ShouldBeTrue();
        It should_detect_if_not_sub = () => FileSystemHelper.isInFolder(new DirectoryInfo("C:/sub"), new FileInfo("C:/main/sub/sub2/file.txt")).ShouldBeFalse();
    }
}
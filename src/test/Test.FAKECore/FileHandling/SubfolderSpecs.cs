using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.FileHandling
{
    public class when_detecting_subfodlers
    {
        It should_detect_same_dir = () => FileSystemHelper.isSubfolderOf(new DirectoryInfo("C:\\sub"), new DirectoryInfo("C:\\sub")).ShouldBeTrue();
        It should_detect_same_dir_slashes = () => FileSystemHelper.isSubfolderOf(new DirectoryInfo("C:\\sub\\"), new DirectoryInfo("C:/sub")).ShouldBeTrue();
        It should_detect_1_level = () => FileSystemHelper.isSubfolderOf(new DirectoryInfo("C:/sub"), new DirectoryInfo("C:\\sub\\sub1")).ShouldBeTrue();
        It should_detect_2_levels = () => FileSystemHelper.isSubfolderOf(new DirectoryInfo("C:/sub"), new DirectoryInfo("C:\\sub\\sub1\\sub2")).ShouldBeTrue();
        It should_detect_if_not_sub = () => FileSystemHelper.isSubfolderOf(new DirectoryInfo("C:/sub"), new DirectoryInfo("C:\\main\\sub\\sub2")).ShouldBeFalse();
    }
}
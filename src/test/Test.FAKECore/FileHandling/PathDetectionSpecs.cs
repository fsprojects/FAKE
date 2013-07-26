using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.FileHandling
{
    public class when_detecting_directories
    {
        It should_detect_temp_dir = () => FileSystemHelper.isDirectory("C:\\temp1\\").ShouldBeTrue();
        It should_not_detect_temp_dir_as_file = () => FileSystemHelper.isFile("C:\\temp1\\").ShouldBeFalse();
    }


    public class when_detecting_files
    {
        It should_detect_temp_file = () => FileSystemHelper.isFile("C:\\temp1\\temp.tmp").ShouldBeTrue();
        It should_not_detect_temp_file_as_dir = () => FileSystemHelper.isDirectory("C:\\temp1\\temp.tmp").ShouldBeFalse();
    }
}
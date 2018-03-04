using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.FileHandling
{
    public class when_comparing_nonidentical_files
    {
        static readonly FileInfo File1 = new FileInfo("TestData/AllObjects.txt");
        static readonly FileInfo File2 = new FileInfo("TestData/AllObjects_2.txt");

        It should_not_be_identical = () => FileHelper.FilesAreEqual(File1, File2).ShouldBeFalse();
    }

    public class when_comparing_identical_files
    {
        static readonly FileInfo File1 = new FileInfo("TestData/AllObjects.txt");
        static readonly FileInfo File2 = new FileInfo("TestData/AllObjects.txt");

        It should_be_identical = () => FileHelper.FilesAreEqual(File1, File2).ShouldBeTrue();
    }
}
using System.IO;
using Fake;
using NUnit.Framework;
using Test.Git;

namespace Test.FAKECore.FileHandling
{
    [TestFixture]
    public class TestCompareFiles
    {
        [Test]
        public void CanFindDifferencesInFiles()
        {
            var fi = new FileInfo(@"TestData\AllObjects.txt");
            var fi2 = new FileInfo(@"TestData\AllObjects_2.txt");
            FileHelper.FilesAreEqual(fi, fi2).ShouldBeFalse();
        }        
        
        [Test]
        public void CanCompareIdenticalFiles()
        {
            var fi = new FileInfo(@"TestData\AllObjects.txt");
            var fi2 = new FileInfo(@"TestData\AllObjects.txt");
            FileHelper.FilesAreEqual(fi, fi2).ShouldBeTrue();
        }
    }
}
using System.IO;
using Fake;
using NUnit.Framework;

namespace Test.FAKECore.FileHandling
{
    [TestFixture]
    public class TestCopyDir
    {
        /// <summary>
        /// Tests the CopyDir Function without filter.
        /// </summary>
        [Test]
        public void TestCopyDirWithoutFilter()
        {
            BaseFunctions.CreateTestFileStructure();

            Assert.IsNotEmpty(Directory.GetFiles(TestData.TestDir, "*.*", SearchOption.AllDirectories));
            string target = string.Format("{0}\\CopyTo", TestData.TestDir);
            BaseFunctions.TestIfDirIsEmpty(target);
            FileHelper.CopyDir(target, TestData.SubDir1, BaseFunctions.AllFilesFunction);

            Assert.AreEqual(3, Directory.GetFiles(target, "*.*", SearchOption.AllDirectories).Length);
        }

        /// <summary>
        /// Tests the CopyDir Function without filter.
        /// </summary>
        [Test]
        public void TestCopyDirWithSubDirectoriesWithoutFilter()
        {
            BaseFunctions.CreateTestFileStructure();

            Assert.IsNotEmpty(Directory.GetFiles(TestData.TestDir, "*.*", SearchOption.AllDirectories));
            string target = string.Format("{0}\\CopyTo", TestData.TestDir);
            BaseFunctions.TestIfDirIsEmpty(target);
            FileHelper.CopyDir(target, TestData.SubDir7, BaseFunctions.AllFilesFunction);

            Assert.AreEqual(6, Directory.GetFiles(target, "*.*", SearchOption.AllDirectories).Length);
            Assert.AreEqual(1, Directory.GetDirectories(target, "*", SearchOption.AllDirectories).Length);
        }
    }
}
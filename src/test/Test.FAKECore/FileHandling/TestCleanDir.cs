using System.IO;
using NUnit.Framework;

namespace Test.FAKECore.FileHandling
{
    [TestFixture]
    public class TestCleanDir
    {
        /// <summary>
        /// Tests the CleanDir function after creating directories.
        /// </summary>
        [Test]
        public void TestCleanDirAfterCreatingDirectories()
        {
            BaseFunctions.CreateTestDirStructure();

            Assert.AreEqual(16, Directory.GetDirectories(TestData.TestDir, "*", SearchOption.AllDirectories).Length);
            Assert.AreEqual(0, Directory.GetFiles(TestData.TestDir, "*.*", SearchOption.AllDirectories).Length);

            BaseFunctions.CleanDir(TestData.TestDir);

            BaseFunctions.TestIfDirIsEmpty(TestData.TestDir);
        }

        /// <summary>
        /// Tests the CleanDir function after creating directories and files.
        /// </summary>
        [Test]
        public void TestCleanDirAfterCreatingDirectoriesAndFiles()
        {
            BaseFunctions.CreateTestFileStructure();

            Assert.AreEqual(16, Directory.GetDirectories(TestData.TestDir, "*", SearchOption.AllDirectories).Length);
            Assert.AreEqual(17, Directory.GetFiles(TestData.TestDir, "*.*", SearchOption.AllDirectories).Length);

            BaseFunctions.CleanDir(TestData.TestDir);

            BaseFunctions.TestIfDirIsEmpty(TestData.TestDir);
        }

        /// <summary>
        /// Tests the cleaned dir is writeable.
        /// </summary>
        [Test]
        public void TestCleanedDirIsWriteable()
        {
            BaseFunctions.CreateTestFileStructure();

            // now clean
            BaseFunctions.CleanDir(TestData.TestDir);

            var di = new DirectoryInfo(TestData.TestDir);

            // Test if not readonly
            Assert.AreEqual(FileAttributes.Directory, di.Attributes);
        }
    }
}
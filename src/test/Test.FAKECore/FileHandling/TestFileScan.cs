using System.IO;
using Fake;
using NUnit.Framework;

namespace Test.FAKECore.FileHandling
{
    [TestFixture]
    public class TestFileScan : BaseTest
    {
        #region Setup/Teardown

        /// <summary>
        ///   Cleans the test dir.
        /// </summary>
        [SetUp]
        public void CleanTestDir()
        {
            BaseFunctions.CleanDir(TestData.TestDir);

            Assert.AreEqual(0, Directory.GetDirectories(TestData.TestDir).Length);
            Assert.AreEqual(0, Directory.GetFiles(TestData.TestDir, "*.*", SearchOption.AllDirectories).Length);
        }

        #endregion

        /// <summary>
        ///   Creates some files and scan them.
        /// </summary>
        [Test]
        public void CreateSomeFilesAndScanThem()
        {
            BaseFunctions.CreateTestFileStructure();

            Assert.AreEqual(3, BaseFunctions.ScanCount("**/file1.nav"));
            Assert.AreEqual(5, BaseFunctions.ScanCount("**/file1.*"));
            Assert.AreEqual(11, BaseFunctions.ScanCount("**/file?.n??"));
            Assert.AreEqual(6, BaseFunctions.ScanCount("**/Sub1/**/file*.*"));
            Assert.AreEqual(7, BaseFunctions.ScanCount("**/*.nav"));
            Assert.AreEqual(0, BaseFunctions.ScanCount("*.nav"));
            Assert.AreEqual(5, BaseFunctions.ScanCount("Test/*.*"));
        }

        /// <summary>
        ///   Creates some files and scan them.
        /// </summary>
        [Test]
        public void CreateSomeFilesAndScanThemWithConcretePath()
        {
            BaseFunctions.CreateTestFileStructure();

            Assert.AreEqual(3, BaseFunctions.ScanCount(FileSetHelper.DefaultBaseDir + "/**/file1.nav"));
            Assert.AreEqual(5, BaseFunctions.ScanCount(FileSetHelper.DefaultBaseDir + "/**/file1.*"));
            Assert.AreEqual(11, BaseFunctions.ScanCount(FileSetHelper.DefaultBaseDir + "/**/file?.n??"));
            Assert.AreEqual(6, BaseFunctions.ScanCount(FileSetHelper.DefaultBaseDir + "/Test/**/Sub1/**/file*.*"));
            Assert.AreEqual(7, BaseFunctions.ScanCount(FileSetHelper.DefaultBaseDir + "/Test/**/*.nav"));
            Assert.AreEqual(0, BaseFunctions.ScanCount(FileSetHelper.DefaultBaseDir + "/*.nav"));
            Assert.AreEqual(5, BaseFunctions.ScanCount(FileSetHelper.DefaultBaseDir + "/Test/*.*"));
        }
    }
}
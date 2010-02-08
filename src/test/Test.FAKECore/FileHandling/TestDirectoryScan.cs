using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Test.FAKECore.FileHandling
{
    [TestFixture]
    public class TestDirectoryScan
    {
        #region Setup/Teardown

        /// <summary>
        /// Cleans the test dir.
        /// </summary>
        [SetUp]
        public void CleanTestDir()
        {
            BaseFunctions.CleanDir(TestData.TestDir);

            Assert.AreEqual(0, Directory.GetDirectories(TestData.TestDir).Length);
            Assert.AreEqual(0, Directory.GetFiles(
                                   TestData.TestDir,
                                   "*.*",
                                   SearchOption.AllDirectories).Length);
        }

        #endregion

        /// <summary>
        /// Creates some directories and scan them.
        /// </summary>
        [Test]
        public void CreateSomeDirectoriesAndScanThem()
        {
            BaseFunctions.CreateTestDirStructure();

            string[] dirs = Directory.GetDirectories(TestData.TestDir, "*", SearchOption.AllDirectories);
            Assert.AreEqual(16, dirs.Count());

            List<string> scanned = BaseFunctions.Scan("Test");
            Assert.AreEqual(TestData.TestDir, scanned.First());
            Assert.AreEqual(1, scanned.Count());

            Assert.AreEqual(1, BaseFunctions.ScanCount("**/Dir2"));
            Assert.AreEqual(6, BaseFunctions.ScanCount("**/**/Sub1"));
            Assert.AreEqual(6, BaseFunctions.ScanCount("**/Sub1"));
            Assert.AreEqual(1, BaseFunctions.ScanCount("**/Sub2/Sub1"));
            Assert.AreEqual(2, BaseFunctions.ScanCount("**/Sub2"));
        }

        /// <summary>
        /// Finds the test dir in base dir.
        /// </summary>
        [Test]
        public void FindTestDirInBaseDir()
        {
            IEnumerable<string> files = BaseFunctions.Scan(@"Test");
            Assert.AreEqual(TestData.TestDir, files.First());
        }
    }
}
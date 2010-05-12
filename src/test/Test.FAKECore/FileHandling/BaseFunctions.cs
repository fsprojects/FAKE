using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fake;
using Microsoft.FSharp.Core;
using NUnit.Framework;

namespace Test.FAKECore.FileHandling
{
    [TestFixture]
    public class BaseFunctions : BaseTest
    {
        /// <summary>
        /// Cleans the dir.
        /// </summary>
        /// <param name="dir">The dir.</param>
        public static void CleanDir(string dir)
        {
            FileHelper.CleanDir(dir);
        }


        /// <summary>
        /// Creates the test dir structure.
        /// </summary>
        public static void CreateTestDirStructure()
        {
            CleanDir(TestData.TestDir);
            CleanDir(TestData.TestDir + "\\Dir1");
            CleanDir(TestData.TestDir + "\\Dir2");
            CleanDir(TestData.TestDir + "\\Dir3\\Sub1");
            CleanDir(TestData.TestDir + "\\Dir3\\Sub2");
            CleanDir(TestData.TestDir + "\\Dir4");
            CleanDir(TestData.TestDir + "\\Dir5");
            CleanDir(TestData.TestDir + "\\Dir6\\Sub1");
            CleanDir(TestData.TestDir + "\\Dir6\\Sub1\\Sub1");
            CleanDir(TestData.TestDir + "\\Dir7\\Sub1");
            CleanDir(TestData.TestDir + "\\Dir7\\Sub2\\Sub1");
            CleanDir(TestData.TestDir + "\\Dir8\\Sub1");
        }

        /// <summary>
        /// Tests if dir is empty or doesn't exists.
        /// </summary>
        /// <param name="dir">The dir.</param>
        public static void TestIfDirIsEmpty(string dir)
        {
            var di = new DirectoryInfo(dir);
            if (!di.Exists) return;
            Assert.AreEqual(0, Directory.GetDirectories(dir, "*", SearchOption.AllDirectories).Length);
            Assert.AreEqual(0, Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories).Length);
        }

        /// <summary>
        /// Creates a test file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="text">The text.</param>
        public static void CreateTestFile(string path, string text)
        {
            TraceHelper.trace(string.Format("Creating test file: {0}", path));
            using (var sw = new StreamWriter(path))
                sw.Write(text);

            var fi = new FileInfo(path);
            if (!fi.Exists)
                throw new FileNotFoundException(path);
        }

        /// <summary>
        /// Creates a test file.
        /// </summary>
        /// <param name="path">The path.</param>
        public static void CreateTestFile(string path)
        {
            CreateTestFile(path, "Hello file system world!");
        }

        /// <summary>
        /// Gets all files function.
        /// </summary>
        /// <value>All files function.</value>
        public static FSharpFunc<string, bool> AllFilesFunction
        {
            get { return FuncConvert.ToFSharpFunc(new Converter<string, bool>(FileHelper.allFiles)); }
        }

        /// <summary>
        /// Scans the specified pattern.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns></returns>
        public static List<string> Scan(string pattern)
        {
            return Scan(pattern, FileSetHelper.DefaultBaseDir);
        }


        /// <summary>
        /// Scans the specified pattern.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <param name="baseDir">The base dir.</param>
        /// <returns></returns>
        public static List<string> Scan(string pattern, string baseDir)
        {
            TraceHelper.trace(string.Format("Scan for {0} in {1}:", pattern, baseDir));

            List<string> list =
                FileSetHelper.Scan(
                    FileSetHelper.SetBaseDir(baseDir,
                                             FileSetHelper.Include(pattern))).ToList();
            foreach (string file in list)
                TraceHelper.trace(string.Format("  - {0}", file));
            return list;
        }

        /// <summary>
        /// Scans the count.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns></returns>
        public static int ScanCount(string pattern)
        {
            return ScanCount(pattern, FileSetHelper.DefaultBaseDir);
        }

        /// <summary>
        /// Scans the count.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <param name="baseDir">The base dir.</param>
        /// <returns></returns>
        public static int ScanCount(string pattern, string baseDir)
        {
            return Scan(pattern, baseDir).Count;
        }

        /// <summary>
        /// Creates the test file structure.
        /// </summary>
        public static void CreateTestFileStructure()
        {
            CreateTestDirStructure();

            CreateTestFile(TestData.TestDir + "\\file1.txt");
            CreateTestFile(TestData.TestDir + "\\file2.fff");
            CreateTestFile(TestData.TestDir + "\\file3.txt");
            CreateTestFile(TestData.TestDir + "\\file3.nav");
            CreateTestFile(TestData.TestDir + "\\file3.nat");
            CreateTestFile(TestData.TestDir + "\\Dir1" + "\\file1.txt");
            CreateTestFile(TestData.TestDir + "\\Dir1" + "\\file2.abc");
            CreateTestFile(TestData.TestDir + "\\Dir1" + "\\file3.atr");
            CreateTestFile(TestData.TestDir + "\\Dir6\\Sub1" + "\\file1.nav");
            CreateTestFile(TestData.TestDir + "\\Dir6\\Sub1" + "\\file2.nat");
            CreateTestFile(TestData.TestDir + "\\Dir6\\Sub1" + "\\file3.nav");

            CreateTestFile(TestData.TestDir + "\\Dir7\\Sub1" + "\\file1.nav");
            CreateTestFile(TestData.TestDir + "\\Dir7\\Sub1" + "\\file2.nat");
            CreateTestFile(TestData.TestDir + "\\Dir7\\Sub1" + "\\file3.nav");

            CreateTestFile(TestData.TestDir + "\\Dir7" + "\\file1.nav");
            CreateTestFile(TestData.TestDir + "\\Dir7" + "\\file2.nat");
            CreateTestFile(TestData.TestDir + "\\Dir7" + "\\file3.nav");
        }


        /// <summary>
        /// Tests the creating of directories.
        /// </summary>
        [Test]
        public void TestDirStructure()
        {
            CreateTestDirStructure();

            Assert.AreEqual(16, Directory.GetDirectories(TestData.TestDir, "*", SearchOption.AllDirectories).Length);
            Assert.AreEqual(0, Directory.GetFiles(TestData.TestDir, "*.*", SearchOption.AllDirectories).Length);
        }


        /// <summary>
        /// Tests the creating of directories and files.
        /// </summary>
        [Test]
        public void TestFileStructure()
        {
            CreateTestFileStructure();

            Assert.AreEqual(16, Directory.GetDirectories(TestData.TestDir, "*", SearchOption.AllDirectories).Length);
            Assert.AreEqual(17, Directory.GetFiles(TestData.TestDir, "*.*", SearchOption.AllDirectories).Length);
        }
    }
}
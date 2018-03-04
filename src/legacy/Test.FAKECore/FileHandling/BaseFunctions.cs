using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Core;

namespace Test.FAKECore.FileHandling
{
    public class when_creating_test_directory_structure : BaseFunctions
    {
        Because of = CreateTestDirStructure;

        It should_create_all_dirs = () => AllDirectories().Length.ShouldEqual(16);

        It should_create_no_files = () => AllFiles().ShouldBeEmpty();
    }

    public class when_creating_test_file_structure : BaseFunctions
    {
        Because of = CreateTestFileStructure;

        It should_create_all_dirs = () => AllDirectories().Length.ShouldEqual(16);

        It should_create_all_files = () => AllFiles().Length.ShouldEqual(17);
    }

    public class BaseFunctions
    {
        private static List<string> DirsToClean = new List<string>
    	{
    		"Dir1",
    		"Dir2",
    		"Dir3/Sub1",
    		"Dir3/Sub2",
    		"Dir4",
    		"Dir5",
    		"Dir6/Sub1",
    		"Dir6/Sub1/Sub1",
    		"Dir7/Sub1",
    		"Dir7/Sub2/Sub1",
    		"Dir8/Sub1"
    	};

		private static List<string> FilesToTest = new List<string>
		{
			"file1.txt",
			"file2.fff",
			"file3.txt",
			"file3.nav",
			"file3.nat",
			"Dir1/file1.txt",
			"Dir1/file2.abc",
			"Dir1/file3.atr",
			"Dir6/Sub1/file1.nav",
			"Dir6/Sub1/file2.nat",
			"Dir6/Sub1/file3.nav",
			"Dir7/Sub1/file1.nav",
			"Dir7/Sub1/file2.nat",
			"Dir7/Sub1/file3.nav",
			"Dir7/file1.nav",
			"Dir7/file2.nat",
			"Dir7/file3.nav"

		};

        /// <summary>
        ///     Gets all files function.
        /// </summary>
        /// <value>All files function.</value>
        public static FSharpFunc<string, bool> AllFilesFunction
        {
            get { return FuncConvert.ToFSharpFunc(new Converter<string, bool>(FileHelper.allFiles)); }
        }

        /// <summary>
        ///     Cleans the dir.
        /// </summary>
        /// <param name="dir">The dir.</param>
        public static void CleanDir(string dir)
        {
            FileHelper.CleanDir(dir);
        }

        /// <summary>
        ///     Creates the test dir structure.
        /// </summary>
        public static void CreateTestDirStructure()
        {
            CleanDir(TestData.TestDir);
            DirsToClean.ForEach(dir => CleanDir(Path.Combine(TestData.TestDir, dir)));
        }

        /// <summary>
        ///     Creates a test file.
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
        ///     Creates a test file.
        /// </summary>
        /// <param name="path">The path.</param>
        public static void CreateTestFile(string path)
        {
            CreateTestFile(path, "Hello file system world!");
        }

        /// <summary>
        ///     Scans the specified pattern.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns></returns>
        public static List<string> Scan(string pattern)
        {
            return Scan(pattern, Path.GetFullPath("."));
        }


        /// <summary>
        ///     Scans the specified pattern.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <param name="baseDir">The base dir.</param>
        /// <returns></returns>
        public static List<string> Scan(string pattern, string baseDir)
        {
            TraceHelper.trace(string.Format("Scan for {0} in {1}:", pattern, baseDir));

            var list = FileSystem.SetBaseDir(baseDir, FileSystem.Include(pattern)).ToList();
            foreach (var file in list)
                TraceHelper.trace(string.Format("  - {0}", file));
            return list;
        }

        /// <summary>
        ///     Scans the count.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns></returns>
        public static int ScanCount(string pattern)
        {
            return ScanCount(pattern, Path.GetFullPath("."));
        }

        /// <summary>
        ///     Scans the count.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <param name="baseDir">The base dir.</param>
        /// <returns></returns>
        public static int ScanCount(string pattern, string baseDir)
        {
            return Scan(pattern, baseDir).Count;
        }

        /// <summary>
        ///     Creates the test file structure.
        /// </summary>
        public static void CreateTestFileStructure()
        {
            CreateTestDirStructure();
            FilesToTest.ForEach(file => CreateTestFile(Path.Combine(TestData.TestDir, file)));
        }


        public static string[] AllDirectories()
        {
            return Directory.GetDirectories(TestData.TestDir, "*", SearchOption.AllDirectories);
        }

        protected static string[] AllFiles()
        {
            return Directory.GetFiles(TestData.TestDir, "*.*", SearchOption.AllDirectories);
        }
    }
}
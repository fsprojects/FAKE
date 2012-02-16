using System;
using System.IO;

namespace Test.FAKECore
{
    public static class TestData
    {
        public static readonly string SideBySideFolder =
            new DirectoryInfo(Environment.CurrentDirectory + @"\SideBySideSpecification\").FullName;

        static TestData()
        {
            BaseDir = Directory.GetCurrentDirectory();
            TestDir = String.Format("{0}\\Test", BaseDir);
            CreateDir(TestDir);

            TestDir2 = String.Format("{0}\\Test2", BaseDir);
            CreateDir(TestDir2);

            SubDir1 = String.Format("{0}\\Dir1", TestDir);
            SubDir7 = String.Format("{0}\\Dir7", TestDir);
            TestDataDir = "TestData\\";
            TestDirectory = new DirectoryInfo(TestDir);
        }

        public static DirectoryInfo TestDirectory { get; set; }

        public static string TestDir { get; set; }

        public static string TestDir2 { get; set; }

        public static string BaseDir { get; set; }

        public static string SubDir1 { get; set; }

        public static string SubDir7 { get; set; }

        public static string TestDataDir { get; set; }

        static void CreateDir(string testDir)
        {
            var di = new DirectoryInfo(testDir);
            if (!di.Exists)
                di.Create();
        }
    }
}
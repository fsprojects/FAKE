using System;
using System.IO;

namespace Test.FAKECore
{
    public static class TestData
    {
        public static readonly string SideBySideFolder =
            new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "SideBySideSpecification")).FullName;

        static TestData()
        {
            BaseDir = Directory.GetCurrentDirectory();
            TestDir = Path.Combine(BaseDir, "Test");
            CreateDir(TestDir);

            TestDir2 = Path.Combine( BaseDir, "Test2");
            CreateDir(TestDir2);

            SubDir1 = Path.Combine( TestDir, "Dir1");
            SubDir7 = Path.Combine(TestDir, "SubDir7");
            TestDataDir = "TestData";
        }

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
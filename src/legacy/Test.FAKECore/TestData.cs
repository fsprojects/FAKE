using System;
using System.IO;

namespace Test.FAKECore
{
    public static class TestData
    {
        public static string SideBySideFolder;

        static TestData()
        {
            try
            {
                BaseDir = Directory.GetCurrentDirectory();
                InitializeFromBase();
            }
            catch (UnauthorizedAccessException)
            {
                // Take temp (for example when running in VS)
                BaseDir = Path.GetTempFileName();
                File.Delete(BaseDir);
                CreateDir(BaseDir);
                InitializeFromBase();
            }
        }

        private static void InitializeFromBase()
        {
            SideBySideFolder =
               new DirectoryInfo(Path.Combine(BaseDir, "SideBySideSpecification")).FullName;
            TestDir = Path.Combine(BaseDir, "Test");
            CreateDir(TestDir);

            TestDir2 = Path.Combine(BaseDir, "Test2");
            CreateDir(TestDir2);

            SubDir1 = Path.Combine(TestDir, "Dir1");
            SubDir7 = Path.Combine(TestDir, "Dir7");
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
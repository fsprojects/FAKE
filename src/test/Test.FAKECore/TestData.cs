using System;
using System.IO;

namespace Test.FAKECore
{
    public static class TestData
    {
        /// <summary>
        ///   Initializes the <see cref = "TestData" /> class.
        /// </summary>
        static TestData()
        {
            BaseDir = Directory.GetCurrentDirectory();
            TestDir = String.Format("{0}\\Test", BaseDir);
            TestDir2 = String.Format("{0}\\Test2", BaseDir);
            SubDir1 = String.Format("{0}\\Dir1", TestDir);
            SubDir7 = String.Format("{0}\\Dir7", TestDir);
        }

        /// <summary>
        ///   Gets or sets the test dir.
        /// </summary>
        /// <value>The test dir.</value>
        public static string TestDir { get; set; }

        /// <summary>
        ///   Gets or sets the test dir.
        /// </summary>
        /// <value>The test dir.</value>
        public static string TestDir2 { get; set; }

        /// <summary>
        ///   Gets or sets the base dir.
        /// </summary>
        /// <value>The base dir.</value>
        public static string BaseDir { get; set; }

        /// <summary>
        ///   Gets or sets the sub dir1.
        /// </summary>
        /// <value>The sub dir1.</value>
        public static string SubDir1 { get; set; }

        /// <summary>
        ///   Gets or sets the sub dir7.
        /// </summary>
        /// <value>The sub dir7.</value>
        public static string SubDir7 { get; set; }

        public static readonly string SideBySideFolder = 
            new DirectoryInfo(Environment.CurrentDirectory + @"\SideBySideSpecification\").FullName;
    }
}
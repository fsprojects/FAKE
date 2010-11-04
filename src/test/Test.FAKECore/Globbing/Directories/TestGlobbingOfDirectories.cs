using System.IO;
using System.Linq;
using Fake;
using NUnit.Framework;
using Test.Git;

namespace Test.FAKECore.Globbing.Directories
{
    [TestFixture]
    public class TestGlobbingOfDirectories
    {
        #region Setup/Teardown

        [SetUp]
        public void ExtractZip()
        {
            FileHelper.CleanDir(TempDir);
            ZipHelper.Unzip(TempDir, "Globbing\\Directories\\SampleApp.zip");
        }

        #endregion

        private const string TempDir = "temptest";

        private static string[] Globbing(string pattern, string baseDir)
        {
            var includes = FileSetHelper.Include(pattern);
            includes = FileSetHelper.SetBaseDir(Path.Combine(TempDir, baseDir), includes);
            return FileSetHelper.ScanImmediately(includes).ToArray();
        }

        [Test]
        public void PatternShouldMatchDir()
        {
            var files = Globbing("SampleApp\\bin\\*", "");

            files.Length.ShouldEqual(3);
            files[0].EndsWith("ilmerge.exclude").ShouldBeTrue();
            files[1].EndsWith("SampleApp.dll").ShouldBeTrue();
        }

        [Test]
        public void PatternShouldMatchBaseDir()
        {
            var files = Globbing("*", "SampleApp\\bin\\");

            files.Length.ShouldEqual(3);
            files[0].EndsWith("ilmerge.exclude").ShouldBeTrue();
            files[1].EndsWith("SampleApp.dll").ShouldBeTrue();
        }
    }
}
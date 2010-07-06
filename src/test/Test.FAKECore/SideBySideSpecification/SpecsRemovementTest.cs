using System.IO;
using Fake.MSBuild;
using NUnit.Framework;
using Test.Git;

namespace Test.FAKECore.SideBySideSpecification
{
    [TestFixture]
    public class SpecsRemovementTest
    {
        private const string Project1 = @"SideBySideSpecification\Project1.txt";
        private const string Project2 = @"SideBySideSpecification\Project2.txt";

        private static void CheckResult(string result, string resultFileName)
        {
            var expected = File.ReadAllText(resultFileName).Replace("\r\n", "\n");
            var actual = File.ReadAllText(result);
            actual.Replace("\r\n", "\n").ShouldEqual(expected);
        }

        [Test]
        public void CanSpliceNUnitReference()
        {
            var result = SpecsRemovement.RemoveAllNUnitReferences(Project1);

            CheckResult(result, @"SideBySideSpecification\Project1_WithoutNUnit.txt");
        }

        [Test]
        public void CanSpliceTestFiles()
        {
            var result = SpecsRemovement.RemoveAllSpecAndTestDataFiles(Project1);

            CheckResult(result, @"SideBySideSpecification\Project1_WithoutTests.txt");
        }

        [Test]
        public void CanSpliceTestFilesAndTestDataFiles()
        {
            var result = SpecsRemovement.RemoveAllSpecAndTestDataFiles(Project2);

            CheckResult(result, @"SideBySideSpecification\Project2_WithoutTests.txt");
        }
    }
}
using System.IO;
using System.Xml.Linq;
using Fake.MSBuild;
using NUnit.Framework;
using Test.Git;

namespace Test.FAKECore.SideBySideSpecification
{
    [TestFixture]
    public class SpecsRemovementTest
    {
        #region Setup/Teardown

        [SetUp]
        public void Setup()
        {
            _project1 = SpecsRemovement.loadProject(@"SideBySideSpecification\Project1.txt");
        }

        #endregion

        private XDocument _project1;

        private static void CheckResult(XDocument result, string resultFileName)
        {
            var spliced = SpecsRemovement.normalize(result);
            var expected = File.ReadAllText(resultFileName).Replace("\r\n", "\n");
            spliced.Replace("\r\n", "\n").ShouldEqual(expected);
        }

        [Test]
        public void CanSpliceNUnitReference()
        {
            var result = SpecsRemovement.removeAssemblyReference(Extensions.Convert<string, bool>(s => s.StartsWith("nunit")),
                                                          _project1);

            CheckResult(result, @"SideBySideSpecification\Project1_WithoutNUnit.txt");
        }

        [Test]
        public void CanSpliceTestFiles()
        {
            var result = SpecsRemovement.removeFiles(Extensions.Convert<string, bool>(s => s.EndsWith("Specs.cs")), _project1);

            CheckResult(result, @"SideBySideSpecification\Project1_WithoutTests.txt");
        }
    }
}
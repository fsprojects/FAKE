using System.IO;
using System.Xml.Linq;
using Fake.MSBuild;
using NUnit.Framework;
using Test.Git;

namespace Test.FAKECore.SideBySideSpecification
{
    [TestFixture]
    public class TestSplicing
    {
        #region Setup/Teardown

        [SetUp]
        public void Setup()
        {
            _project1 = Splicing.loadProject(@"SideBySideSpecification\Project1.txt");
        }

        #endregion

        private XDocument _project1;

        private static void CheckResult(XDocument result, string resultFileName)
        {
            Splicing.normalize(result).ShouldEqual(File.ReadAllText(resultFileName));
        }

        [Test]
        public void CanSpliceNUnitReference()
        {
            var result = Splicing.removeAssemblyReference(_project1,
                                                          Extensions.Convert<string, bool>(s => s.StartsWith("nunit")));

            CheckResult(result, @"SideBySideSpecification\Project1_WithoutNUnit.txt");
        }

        [Test]
        public void CanSpliceTestFiles()
        {
            var result = Splicing.removeFiles(_project1, Extensions.Convert<string, bool>(s => s.EndsWith("Specs.cs")));

            CheckResult(result, @"SideBySideSpecification\Project1_WithoutTests.txt");
        }
    }
}
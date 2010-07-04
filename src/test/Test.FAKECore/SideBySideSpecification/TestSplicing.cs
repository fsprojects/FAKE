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
            var result = Splicing.removeAssemblyReference(Extensions.Convert<string, bool>(s => s.StartsWith("nunit")),
                                                          _project1);

            CheckResult(result, @"SideBySideSpecification\Project1_WithoutNUnit.txt");
        }

        [Test]
        public void CanSpliceTestFiles()
        {
            var result = Splicing.removeFiles(Extensions.Convert<string, bool>(s => s.EndsWith("Specs.cs")), _project1);

            CheckResult(result, @"SideBySideSpecification\Project1_WithoutTests.txt");
        }
    }
}
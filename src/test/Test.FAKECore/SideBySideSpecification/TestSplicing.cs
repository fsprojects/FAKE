using System.IO;
using Fake.MSBuild;
using NUnit.Framework;
using Test.Git;

namespace Test.FAKECore.SideBySideSpecification
{
    [TestFixture]
    public class TestSplicing
    {
        [Test]
        public void CanSpliceNUnitReference()
        {
            var project1 = File.ReadAllText(@"SideBySideSpecification\Project1.txt");
            var project1Result = File.ReadAllText(@"SideBySideSpecification\Project1_WithoutNUnit.txt");

            var result = Splicing.removeAssemblyReference(project1,
                                                          Extensions.Convert<string, bool>(s => s.StartsWith("nunit")));
            result.ShouldEqual(project1Result);
        }
    }
}
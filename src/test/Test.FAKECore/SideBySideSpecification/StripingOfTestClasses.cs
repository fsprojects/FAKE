using System.IO;
using Fake;
using Fake.MSBuild;
using NUnit.Framework;
using Test.Git;

namespace Test.FAKECore.SideBySideSpecification
{
    [TestFixture]
    public class StripingOfTestClasses
    {
        [Test]
        public void StripingOfTests()
        {
            var project1= File.ReadAllText("Project1.txt");
            var project1Result = File.ReadAllText("Project1_WithoutNUnit.txt");

            var result = Splicing.removeAssemblyReference(project1);
            result.ShouldEqual(project1Result);
        }
    }
}
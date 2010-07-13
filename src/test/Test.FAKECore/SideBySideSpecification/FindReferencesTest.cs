using System.Collections.Generic;
using System.Linq;
using Fake;
using NUnit.Framework;
using Test.Git;

namespace Test.FAKECore.SideBySideSpecification
{
    [TestFixture]
    public class FindReferencesTest
    {
        private const string Project1 = @"SideBySideSpecification\Project1.txt";
        private const string ProjectWithProjectReference = @"SideBySideSpecification\Project_WithProjectReference.txt";

        private static List<string> GetProjectReferences(string projectFileName)
        {
            return MSBuildHelper.getProjectReferences(MSBuildHelper.loadProject(projectFileName)).ToList();
        }

        [Test]
        public void CanFindReferenceInProjectWithReference()
        {
            var references = GetProjectReferences(ProjectWithProjectReference);
            references.Count.ShouldEqual(2);
            references[0].ShouldEqual(@"..\FakeLib\FakeLib.fsproj");
            references[1].ShouldEqual(@"..\Test\FakeLibBla.fsproj");
        }

        [Test]
        public void ReferencesInProject1ShouldBeEmpty()
        {
            GetProjectReferences(Project1).ShouldBeEmpty();
        }
    }
}
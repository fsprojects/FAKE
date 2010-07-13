using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fake;
using NUnit.Framework;
using Test.Git;

namespace Test.FAKECore.SideBySideSpecification
{
    [TestFixture]
    public class FindReferencesTest
    {
        private readonly string _folder = (new DirectoryInfo(Environment.CurrentDirectory + @"\SideBySideSpecification\")).FullName;
        private const string Project1 = @"SideBySideSpecification\Project1.txt";
        private const string ProjectWithProjectReference = @"SideBySideSpecification\Project_WithProjectReference.txt";

        private const string ProjectWithRecursiveProjectReference =
            @"SideBySideSpecification\Project_WithRecursiveProjectReference.txt";

        private static List<string> GetProjectReferences(string projectFileName)
        {
            return MSBuildHelper.getProjectReferences(projectFileName).ToList();
        }

        [Test]
        public void CanFindReferencesInProjectWithRecursiveReferences()
        {
            var references = GetProjectReferences(ProjectWithRecursiveProjectReference);
            references.Count.ShouldEqual(3);
            references.ShouldContain(_folder + @"Project_WithProjectReference.txt");
            references.ShouldContain(_folder + @"Project1.txt");
            references.ShouldContain(_folder + @"Project2.txt");
        }

        [Test]
        public void CanFindReferencesInProjectWithReferences()
        {
            var references = GetProjectReferences(ProjectWithProjectReference);
            references.Count.ShouldEqual(2);
            references.ShouldContain(_folder + @"Project1.txt");
            references.ShouldContain(_folder + @"Project2.txt");
        }

        [Test]
        public void ReferencesInProject1ShouldBeEmpty()
        {
            GetProjectReferences(Project1).ShouldBeEmpty();
        }
    }
}
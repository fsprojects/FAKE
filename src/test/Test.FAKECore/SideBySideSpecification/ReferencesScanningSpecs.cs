using System.Collections.Generic;
using System.Linq;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.SideBySideSpecification
{
    public class when_searching_references_in_project_with_recursive_references
    {        
        Because of = () => _references = MSBuildHelper.getProjectReferences(@"SideBySideSpecification\Project_WithRecursiveProjectReference.txt").ToList();

        It should_find_3_references = () => _references.Count.ShouldEqual(3);
        It should_find_project_with_reference = () => _references.ShouldContain(TestData.SideBySideFolder + @"Project_WithProjectReference.txt");
        It should_find_project1 = () => _references.ShouldContain(TestData.SideBySideFolder + @"Project1.txt");
        It should_find_project2 = () => _references.ShouldContain(TestData.SideBySideFolder + @"Project2.txt");

        private static List<string> _references;
    }

    public class when_searching_references_in_project_with_references
    {
        Because of = () => _references = MSBuildHelper.getProjectReferences(@"SideBySideSpecification\Project_WithProjectReference.txt").ToList();

        It should_find_2_references = () => _references.Count.ShouldEqual(2);
        It should_find_project1 = () => _references.ShouldContain(TestData.SideBySideFolder + @"Project1.txt");
        It should_find_project2 = () => _references.ShouldContain(TestData.SideBySideFolder + @"Project2.txt");
        
        private static List<string> _references;
    }

    public class when_searching_references_in_project_without_references
    {
        Because of = () => _references = MSBuildHelper.getProjectReferences(@"SideBySideSpecification\Project1.txt").ToList();

        It should_not_find_references = () => _references.ShouldBeEmpty();
        
        private static List<string> _references;
    }
}
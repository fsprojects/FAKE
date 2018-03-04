using System.Collections.Generic;
using Fake.MSBuild;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_fixing_missing_files
    {
        const string Project = "ProjectTestFiles/FakeLib.fsproj";
        const string Project4 = "ProjectTestFiles/FakeLib4.fsproj";

        Because of = () =>
        {
            ProjectSystem.FixMissingFiles(Project, new List<string> {Project4});
            _project = ProjectSystem.ProjectFile.FromFile(Project4);
        };

        It should_have_added_the_missing_files = () =>
        {
            _project.Files.ShouldContain("Git\\Merge.fs");
            _project.Files.ShouldContain("Git\\Stash.fs");
        };

        static ProjectSystem.ProjectFile _project;
    }

    public class when_fixing_duplicate_files
    {
        const string Project = "ProjectTestFiles/FakeLib.fsproj";
        const string Project6 = "ProjectTestFiles/FakeLib6.fsproj";

        Because of = () =>
        {
            ProjectSystem.FixProjectFiles(Project, new List<string> { Project6 });
            _project = ProjectSystem.ProjectFile.FromFile(Project6);
        };

        It should_have_removed_the_duplicate_files = () => _project.FindDuplicateFiles().ShouldBeEmpty();
        
        static ProjectSystem.ProjectFile _project;
    }
}
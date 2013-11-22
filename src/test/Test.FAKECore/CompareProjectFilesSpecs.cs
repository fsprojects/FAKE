using System;
using System.Collections.Generic;
using System.Linq;
using Fake.MSBuild;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_searching_for_missing_files_in_project2
    {
        const string Project = "ProjectTestFiles/FakeLib.fsproj";
        const string Project2 = "ProjectTestFiles/FakeLib2.fsproj";
        static ProjectSystem.ProjectComparison _missing;

        Because of = () => _missing = ProjectSystem.findMissingFiles(Project, new List<string> {Project2}).First();

        It should_detect_missing_files_in_project2 = () =>
        {
            _missing.ProjectFileName.ShouldEqual(Project2);
            _missing.MissingFiles.Count().ShouldEqual(2);
            _missing.MissingFiles.ShouldContain("Git\\Merge.fs");
            _missing.MissingFiles.ShouldContain("Git\\Stash.fs");
        };
    }

    public class when_searching_for_duplicate_files
    {
        const string Project = "ProjectTestFiles/FakeLib.fsproj";
        const string Project2 = "ProjectTestFiles/FakeLib5.fsproj";
        static ProjectSystem.ProjectComparison _missing;

        Because of = () => _missing = ProjectSystem.findMissingFiles(Project, new List<string> { Project2 }).First();

        It should_detect_missing_files_in_project2 = () =>
        {
            _missing.ProjectFileName.ShouldEqual(Project2);
            _missing.DuplicateFiles.Count().ShouldEqual(1);
            _missing.DuplicateFiles.ShouldContain("Git\\CommitMessage.fs");
        };
    }

    public class when_comparing_with_missing_files
    {
        const string Project = "ProjectTestFiles/FakeLib.fsproj";
        const string Project2 = "ProjectTestFiles/FakeLib2.fsproj";
        static Exception _exn;

        Because of =
            () => _exn = Catch.Exception(() => ProjectSystem.CompareProjectsTo(Project, new List<string> { Project2 }));

        It should_fire_useful_exception = () =>
        {
            _exn.Message.ShouldContain("Missing");
            _exn.Message.ShouldContain("ProjectTestFiles/FakeLib2.fsproj");
            _exn.Message.ShouldContain("Git\\Merge.fs");
            _exn.Message.ShouldContain("Git\\Stash.fs");
        };
    }

    public class when_comparing_with_unordered_files
    {
        const string Project = "ProjectTestFiles/FakeLib3.fsproj";
        const string Project2 = "ProjectTestFiles/FakeLib2.fsproj";
        static Exception _exn;

        Because of =
            () => _exn = Catch.Exception(() => ProjectSystem.CompareProjectsTo(Project, new List<string> { Project2 }));

        It should_fire_useful_exception = () =>
        {
            _exn.Message.ShouldContain("Unordered");
            _exn.Message.ShouldContain("ProjectTestFiles/FakeLib2.fsproj");
            _exn.Message.ShouldContain("MSBuild\\SpecsRemover.fs");
            _exn.Message.ShouldContain("MSBuild\\SpecsRemovement.fs");
        };
    }

    public class when_comparing_with_duplictae_files
    {
        const string Project = "ProjectTestFiles/FakeLib.fsproj";
        const string Project2 = "ProjectTestFiles/FakeLib5.fsproj";
        static Exception _exn;

        Because of =
            () => _exn = Catch.Exception(() => ProjectSystem.CompareProjectsTo(Project, new List<string> { Project2 }));

        It should_fire_useful_exception = () =>
        {
            _exn.Message.ShouldContain("Duplicate");
            _exn.Message.ShouldContain("ProjectTestFiles/FakeLib5.fsproj");
            _exn.Message.ShouldContain("Git\\CommitMessage.fs");
        };
    }

    public class when_searching_for_missing_files_in_project1
    {
        const string Project = "ProjectTestFiles/FakeLib.fsproj";
        const string Project2 = "ProjectTestFiles/FakeLib2.fsproj";
        static ProjectSystem.ProjectComparison _missing;

        Because of = () => _missing = ProjectSystem.findMissingFiles(Project2, new List<string> {Project}).First();

        It should_detect_missing_files_in_project1 = () =>
        {
            _missing.ProjectFileName.ShouldEqual(Project);
            _missing.MissingFiles.Count().ShouldEqual(1);
            _missing.MissingFiles.ShouldContain("Messages.fs");
        };
    }

    public class when_searching_for_unordered_files_in_fsproj
    {
        const string Project3 = "ProjectTestFiles/FakeLib3.fsproj";
        const string Project2 = "ProjectTestFiles/FakeLib2.fsproj";
        static ProjectSystem.ProjectComparison _missing;

        Because of = () => _missing = ProjectSystem.findMissingFiles(Project2, new List<string> { Project3 }).First();

        It should_detect_unordered_files = () =>
        {
            _missing.ProjectFileName.ShouldEqual(Project3);
            _missing.UnorderedFiles.Count().ShouldEqual(2);
            _missing.UnorderedFiles.ShouldContain("MSBuild\\SpecsRemover.fs");
            _missing.UnorderedFiles.ShouldContain("MSBuild\\SpecsRemovement.fs");
        };
    }

    public class when_searching_for_unordered_files_in_csproj
    {
        const string Project3 = "ProjectTestFiles/FakeLib3.csproj";
        const string Project2 = "ProjectTestFiles/FakeLib2.csproj";
        static ProjectSystem.ProjectComparison _missing;

        It should_not_detect_unordered_files = () => ProjectSystem.findMissingFiles(Project2, new List<string> { Project3 }).ShouldBeEmpty();
    }
}
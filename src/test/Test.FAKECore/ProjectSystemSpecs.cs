using System;
using System.Collections.Generic;
using System.IO;
using Fake.MSBuild;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_checking_for_files
    {
        static ProjectSystem.ProjectFile _project;
        Because of = () => _project = ProjectSystem.ProjectFile.FromFile("ProjectTestFiles/FakeLib.fsproj");

        It should_find_the_SpecsRemovement_helper_in_the_MSBuild_folder = () =>
            _project.Files.ShouldContain("MSBuild\\SpecsRemovement.fs");

        It should_find_the_SpecsRemover_which_has_some_strange_xml = () =>
            _project.Files.ShouldContain("MSBuild\\SpecsRemover.fs");

        It should_find_the_semver_helper = () =>
            _project.Files.ShouldContain("SemVerHelper.fs");

        It should_not_find_the_SpecsRemovement_helper = () =>
            _project.Files.ShouldNotContain("SpecsRemovement.fs");

        It should_not_find_the_badadadum_helper = () =>
            _project.Files.ShouldNotContain("badadadumHelper.fs");
    }

    public class when_adding_files
    {
        static ProjectSystem.ProjectFile _project;
        static ProjectSystem.ProjectFile _project2;
        Establish context = () => _project = ProjectSystem.ProjectFile.FromFile("ProjectTestFiles/FakeLib.fsproj");
        Because of = () => _project2 = _project.AddFile("badadadumHelper.fs");

        It should_find_the_SpecsRemovement_helper_in_the_MSBuild_folder = () =>
            _project2.Files.ShouldContain("MSBuild\\SpecsRemovement.fs");

        It should_find_the_badadadum_helper = () =>
            _project2.Files.ShouldContain("badadadumHelper.fs");

        It should_work_immutable = () =>
            _project.Files.ShouldNotContain("badadadumHelper.fs");
    }

    public class when_removing_files
    {
        static ProjectSystem.ProjectFile _project;
        static ProjectSystem.ProjectFile _project2;
        Establish context = () => _project = ProjectSystem.ProjectFile.FromFile("ProjectTestFiles/FakeLib5.fsproj");
        Because of = () => _project2 = _project.RemoveFile("RegAsmHelper.fs");

        It should_not_have_the_file = () =>
            _project2.Files.ShouldNotContain("RegAsmHelper.fs");

        It should_still_contain_other_files = () =>
            _project2.Files.ShouldContain("NuGetHelper.fs");

        It should_work_immutable = () =>
            _project.Files.ShouldContain("RegAsmHelper.fs");

    }

    public class when_removing_duplicate_files
    {
        static ProjectSystem.ProjectFile _project;
        static ProjectSystem.ProjectFile _project2;
        Establish context = () => _project = ProjectSystem.ProjectFile.FromFile("ProjectTestFiles/FakeLib5.fsproj");
        Because of = () => _project2 = _project.RemoveDuplicates();

        It should_not_have_duplicates = () =>
            _project2.FindDuplicateFiles().ShouldBeEmpty();

        It should_still_contain_the_file = () =>
            _project2.Files.ShouldContain("Git\\CommitMessage.fs");

        It should_work_immutable = () =>
            _project.FindDuplicateFiles().ShouldContain("Git\\CommitMessage.fs");

    }

    public class when_removing_compile_nodes_with_missing_files
    {
        const string ProjectFilePath = @"ProjectTestFiles/CSharpApp.csproj";
        private static ProjectSystem.ProjectFile _project;

        private Because of = () =>
        {
            Func<string, bool> fileExists = s =>
            {
                // We have to use Path.Combine here to work x-plat.
                var pathsToRemove = new List<string>() {Path.Combine("ProjectTestFiles", "Class1.cs"), Path.Combine("ProjectTestFiles", "Folder", "FolderFile2.cs")};
                return !pathsToRemove.Exists(pathToRemove => s.Equals(pathToRemove, StringComparison.InvariantCulture));
            };
            var projectFile = ProjectSystem.ProjectFile.FromFile(ProjectFilePath);
            _project = ProjectSystem.removeCompileNodesWithMissingFiles(fileExists.Convert(), projectFile);
        };

        It should_delete_missing_files_in_csharpapp = () =>
        {
            _project.Files.ShouldNotContain(new []{"Class1.cs", @"Folder\FolderFile2.cs"});
            // We DON'T have to use Path.Combine here, because the CsProj paths are same on both plats for our test proj.
            _project.Files.ShouldContain(new [] {@"Folder\FolderFile1.cs", @"Program.cs", @"Properties\AssemblyInfo.cs"});
        };
    }
}
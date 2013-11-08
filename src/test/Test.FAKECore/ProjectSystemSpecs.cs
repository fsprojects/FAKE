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
        Establish context = () => _project = ProjectSystem.ProjectFile.FromFile("ProjectTestFiles/FakeLib.fsproj");
        Because of = () => _project = _project.AddFile("badadadumHelper.fs");

        It should_find_the_SpecsRemovement_helper_in_the_MSBuild_folder = () =>
            _project.Files.ShouldContain("MSBuild\\SpecsRemovement.fs");

        It should_find_the_badadadum_helper = () =>
            _project.Files.ShouldContain("badadadumHelper.fs");
    }
}
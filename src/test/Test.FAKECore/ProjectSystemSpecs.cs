using Fake.MsBuild;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_checking_for_files
    {
        Because of = () => _project = new ProjectSystem.ProjectSystem("ProjectTestFiles/FakeLib.proj");

        It should_find_the_semver_helper = () =>
            _project.FileExistsInProject("SemVerHelper.fs").ShouldBeTrue();

        It should_not_find_the_SpecsRemovement_helper = () =>
            _project.FileExistsInProject("SpecsRemovement.fs").ShouldBeFalse();

        It should_find_the_SpecsRemovement_helper_in_the_MSBuild_folder = () =>
            _project.FileExistsInProject("MSBuild/SpecsRemovement.fs").ShouldBeTrue();

        It should_find_the_SpecsRemovement_helper_in_the_MSBuild_folder_with_backslash = () =>
            _project.FileExistsInProject("MSBuild\\SpecsRemovement.fs").ShouldBeTrue();

        It should_not_find_the_badadadum_helper = () =>
            _project.FileExistsInProject("badadadumHelper.fs").ShouldBeFalse();

        static ProjectSystem.ProjectSystem _project;
    }
}
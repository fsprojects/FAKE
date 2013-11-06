using Fake.MsBuild;
using Machine.Specifications;
using NuGet;

namespace Test.FAKECore
{
    public class when_checking_for_files
    {
        static IProjectSystem _project;
        Because of = () => _project = new ProjectSystem.ProjectSystem("ProjectTestFiles/FakeLib.proj") as IProjectSystem;

        It should_find_the_semver_helper = () =>
            _project.FileExistsInProject("SemVerHelper.fs").ShouldBeTrue();
    }
}
using System.Collections.Generic;
using System.Linq;
using Fake;
using Machine.Specifications;

namespace Test.Fake.Deploy.PackageMgt
{
    public class when_getting_all_active_releases_from_test_folder
    {
        static IEnumerable<NuGetHelper.NuSpecPackage> _activeReleases;
        Because of = () => _activeReleases = DeploymentHelper.getActiveReleases(TestData.TestDataDir);

        It should_contain_a_SignalR_releases = () => _activeReleases.Where(x => x.Id == "SignalR").ShouldNotBeEmpty();
        It should_contain_a_jQuery_releases = () => _activeReleases.Where(x => x.Id == "jQuery").ShouldNotBeEmpty();
        It should_contain_two_active_releases = () => _activeReleases.Count().ShouldEqual(2);
    }

    public class when_getting_active_jQuery_release_from_test_folder
    {
        static NuGetHelper.NuSpecPackage _release;
        Because of = () => _release = DeploymentHelper.getActiveReleaseFor(TestData.TestDataDir, "jQuery");

        It should_the_right_id = () => _release.Id.ShouldEqual("jQuery");
        It should_the_right_version = () => _release.Version.ShouldEqual("1.7.1");
    }

    public class when_getting_all_releases_from_test_folder
    {
        static IEnumerable<NuGetHelper.NuSpecPackage> _releases;
        Because of = () => _releases = DeploymentHelper.getAllReleases(TestData.TestDataDir);

        It should_contain_3_SignalR_releases = () => _releases.Count(x => x.Id == "SignalR").ShouldEqual(3);
        It should_contain_3_jQuery_releases = () => _releases.Count(x => x.Id == "jQuery").ShouldEqual(3);
        It should_contain_six_releases = () => _releases.Count().ShouldEqual(6);
    }

    public class when_getting_all_SignalR_releases_from_test_folder
    {
        static IEnumerable<NuGetHelper.NuSpecPackage> _releases;
        Because of = () => _releases = DeploymentHelper.getAllReleasesFor(TestData.TestDataDir, "SignalR");

        It should_all_be_SignalR_releases = () => _releases.Count(x => x.Id == "SignalR").ShouldEqual(3);
        It should_contain_three_releases = () => _releases.Count().ShouldEqual(3);
    }
}
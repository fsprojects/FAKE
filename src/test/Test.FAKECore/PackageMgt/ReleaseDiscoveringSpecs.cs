using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.PackageMgt
{
    class when_getting_all_releases_from_test_folder
    {
        Because of = () => _activeReleases = DeploymentHelper.getActiveReleasesInDirectory(TestData.TestDataDir);
        static IEnumerable<NuGetHelper.NuSpecPackage> _activeReleases;

        It should_contain_two_active_releases = () => _activeReleases.Count().ShouldEqual(2);
    }
}

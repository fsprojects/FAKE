using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.PackageMgt
{
    public class when_getting_the_nuget_feed_url
    {
        It should_return_the_package_url = () => NuGetHelper.getRepoUrl().ShouldEqual(NugetData.RepositoryUrl);
    }

    public class when_discovering_the_lastest_FAKE_package
    {
        static NuGetHelper.NuSpecPackage _package;
        Because of = () => _package = NuGetHelper.getLatestPackage(NuGetHelper.getRepoUrl(), "FAKE");

        It should_be_the_latest_version =
            () => _package.IsLatestVersion.ShouldBeTrue();

        It should_contain_steffen_as_author =
            () => _package.Authors.ShouldContain("Steffen Forkmann");

        It should_contain_the_creation_date = 
            () => _package.Created.Year.ShouldBeGreaterThanOrEqualTo(2012);

        It should_contain_the_id = () => _package.Id.ShouldEqual("FAKE");

        It should_contain_the_packet_hash = () => _package.PackageHash.ShouldNotBeNull();

        It should_contain_the_packet_hash_algorithm = 
            () => _package.PackageHashAlgorithm.ShouldEqual("SHA512");

        It should_contain_the_project_url = 
            () => _package.ProjectUrl.ShouldEqual("https://github.com/fsharp/Fake");

        It should_contain_the_publiNuSpecPackageshing_date = 
            () => _package.Published.Year.ShouldBeGreaterThanOrEqualTo(2012);

        It should_contain_the_version = () => _package.Version.ShouldContain(".");

        It should_contain_the_package_url =
            () => _package.Url.ShouldEqual("https://packages.nuget.org/api/v1/package/FAKE/" + _package.Version);

        It should_build_the_FileName_from_id =
            () => _package.FileName.ShouldStartWith("FAKE");        
    }

    public class when_discovering_a_specific_outdated_FAKE_package
    {
        static NuGetHelper.NuSpecPackage _package;
        Because of = () => _package = NuGetHelper.getPackage(NuGetHelper.getRepoUrl(), "FAKE", "1.56.10");

        It should_be_the_latest_version = () => _package.IsLatestVersion.ShouldBeFalse();
        It should_contain_the_id = () => _package.Id.ShouldEqual("FAKE");
        It should_contain_the_version = () => _package.Version.ShouldEqual("1.56.10");
    }
}

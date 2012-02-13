using Fake;
using Machine.Specifications;

namespace Test.FAKECore.NugetFeed
{
    public class when_getting_the_nuget_feed_url
    {
        It should_return_the_package_url = () => NuGetHelper.getRepoUrl().ShouldEqual(NugetData.RepositoryUrl);
    }

    public class when_using_the_lastest_FAKE_package
    {
        static NuGetHelper.NugetFeedPackage _package;
        Because of = () => _package = NuGetHelper.getLatestPackage(NuGetHelper.getRepoUrl(), "FAKE");

        It should_return_URL =
            () => _package.Url.ShouldStartWith("http://packages.nuget.org/api/v1/package/FAKE/");
    }
}
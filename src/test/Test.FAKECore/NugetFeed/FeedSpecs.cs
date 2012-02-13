using Fake;
using Machine.Specifications;

namespace Test.FAKECore.Nuget
{
    public class when_getting_the_nuget_feed_url
    {
        It should_return_the_package_url = () => NuGetHelper.getRepoUrl().ShouldEqual(NugetData.RepositoryUrl);
    }
}
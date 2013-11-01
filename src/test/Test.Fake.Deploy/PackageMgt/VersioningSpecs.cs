using Fake;
using Machine.Specifications;

namespace Test.Fake.Deploy.PackageMgt
{
    public class when_checking_the_given_version
    {
        It should_parse_a_normal_version =
            () => DeploymentHelper.VersionInfo.Parse("0.1.2")
                      .ShouldEqual(DeploymentHelper.VersionInfo.NewSpecific("0.1.2"));

        It should_parse_lower_predecessor =
            () => DeploymentHelper.VersionInfo.Parse("head~7")
                      .ShouldEqual(DeploymentHelper.VersionInfo.NewPredecessor(7));

        It should_parse_the_predecessor =
            () => DeploymentHelper.VersionInfo.Parse("HEAD~1")
                      .ShouldEqual(DeploymentHelper.VersionInfo.NewPredecessor(1));

        It should_parse_the_sixth_predecessor =
            () => DeploymentHelper.VersionInfo.Parse("HEAD~6")
                      .ShouldEqual(DeploymentHelper.VersionInfo.NewPredecessor(6));
    }
}
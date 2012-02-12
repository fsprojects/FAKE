using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.Deployment
{
    public class when_unpacking_a_package
    {
        Because of = () =>
                     DeploymentHelper.unpack(TestData.OutputDir,DeploymentHelper.getPackageFromFile(TestData.GetPackageFile("v1","helloworld")));

        It should_extract_the_package = () => Directory.Exists(TestData.OutputDir + "App").ShouldBeTrue();
    }
}
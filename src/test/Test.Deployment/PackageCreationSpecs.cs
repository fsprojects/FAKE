using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.Deployment
{
    public class TestData
    {
        public static string OutputDir
        {
            get { return "output\\"; }
        }
    }

    public class When_creating_a_package_from_directory
    {
        Because of = () => DeploymentHelper.createDeploymentPackageFromDirectory("helloworld", "0.1", @"packages\HelloWorld\DeployScript.fsx", @"packages\HelloWorld", TestData.OutputDir);

        It should_create_the_package = () => File.Exists(TestData.OutputDir + "helloworld.fakepkg").ShouldBeTrue();
    }
}
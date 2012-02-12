using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.Deployment
{
    public class when_creating_a_package_from_directory
    {
        const string PackageName = "helloworld";
        const string PackageVersion = "0.1";
        static readonly string Path = TestData.OutputDir + "helloworld.fakepkg";

        Because of = () =>
                     DeploymentHelper.createDeploymentPackageFromDirectory(
                         PackageName,
                         PackageVersion,
                         TestData.GetPackageDir("HelloWorld") + "DeployScript.fsx",
                         TestData.GetPackageDir("HelloWorld"),
                         TestData.OutputDir);

        It should_be_parsable_as_json = () =>
        {
            var package = DeploymentHelper.getPackageFromFile(Path);
            package.Key.Id.ShouldEqual(PackageName);
            package.Key.Version.ShouldEqual(PackageVersion);
        };

        It should_create_the_package = () => File.Exists(Path).ShouldBeTrue();
    }
}
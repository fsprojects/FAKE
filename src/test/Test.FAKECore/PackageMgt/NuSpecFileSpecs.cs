using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.PackageMgt
{
    public class when_parsing_the_fake_nuspec_file
    {
        static NuGetHelper.NuSpecPackage _package;
        static string _text;
        Establish context = () => _text = File.ReadAllText(Path.Combine(TestData.TestDataDir, "fake.nuspec"));
        Because of = () => _package = NuGetHelper.getNuspecProperties(_text);

        It should_build_the_DirectoryName_from_id_and_version =
            () => _package.DirectoryName.ShouldEqual("@project@.@build.number@");

        It should_contain_the_authors_placeholder =
            () => _package.Authors.ShouldEqual("@authors@");

        It should_contain_the_description_placeholder =
            () => _package.Description.ShouldEqual("@description@");

        It should_contain_the_license_url =
            () => _package.LicenseUrl.ShouldEqual("http://www.github.com/fsharp/Fake/blob/master/License.txt");

        It should_contain_the_project_placeholder =
            () => _package.Id.ShouldEqual("@project@");

        It should_contain_the_project_url_placeholder =
            () => _package.ProjectUrl.ShouldEqual("http://www.github.com/fsharp/Fake");

        It should_contain_the_version_placeholder =
            () => _package.Version.ShouldEqual("@build.number@");
    }
}
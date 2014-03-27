using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.PackageMgt
{
    [Behaviors]
    public class nuspec_behaviours
    {
        public static NuGetHelper.NuSpecPackage Package;

        It should_build_the_DirectoryName_from_id_and_version =
            () => Package.DirectoryName.ShouldEqual("@project@.@build.number@");

        It should_contain_the_authors_placeholder =
            () => Package.Authors.ShouldEqual("@authors@");

        It should_contain_the_description_placeholder =
            () => Package.Description.ShouldEqual("@description@");

        It should_contain_the_license_url =
            () => Package.LicenseUrl.ShouldEqual("http://www.github.com/fsharp/Fake/blob/master/License.txt");

        It should_contain_the_project_placeholder =
            () => Package.Id.ShouldEqual("@project@");

        It should_contain_the_project_url_placeholder =
            () => Package.ProjectUrl.ShouldEqual("http://www.github.com/fsharp/Fake");

        It should_contain_the_version_placeholder =
            () => Package.Version.ShouldEqual("@build.number@");
        
    }

    public class when_parsing_the_fake_nuspec_file
    {
        static string _text;
        Establish context = () => _text = File.ReadAllText(Path.Combine(TestData.TestDataDir, "fake.nuspec"));
        Because of = () => nuspec_behaviours.Package = NuGetHelper.getNuspecProperties(_text);
        Behaves_like<nuspec_behaviours> nuspec_behaviour;
    }

    public class when_parsing_fake_nuspec_file_with_schema_version_2011_08
    {
        static string _text;
        Establish context = () => _text = File.ReadAllText(Path.Combine(TestData.TestDataDir, "fake_schema_2011_08.nuspec"));
        Because of = () => nuspec_behaviours.Package = NuGetHelper.getNuspecProperties(_text);
        Behaves_like<nuspec_behaviours> nuspec_behaviour;
    }

    public class when_parsing_fake_nuspec_file_with_no_schema
    {
        static string _text;
        Establish context = () => _text = File.ReadAllText(Path.Combine(TestData.TestDataDir, "fake_no_schema.nuspec"));
        Because of = () => nuspec_behaviours.Package = NuGetHelper.getNuspecProperties(_text);
        Behaves_like<nuspec_behaviours> nuspec_behaviour;
    }
}

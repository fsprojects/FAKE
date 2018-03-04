using Fake;
using Machine.Specifications;

namespace Test.Fake.Deploy.PackageMgt
{

    public class when_reading_nuspec_values_with_new_nuspace_namespace
    {
        //static IEnumerable<NuGetHelper.NuSpecPackage> _releases;
        private static NuGetHelper.NuSpecPackage _package = null;
        const string Nuspec = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd"">
  <metadata>
    <id>foo</id>
    <version>0.0.1-alpha</version>
    <authors>ashic</authors>
    <owners>ashic</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>the foo thing</description>
    <copyright>Copyright 2015</copyright>
    <title>foo</title>
    <tags>ci msdeploy</tags>
  </metadata>
  <files>
    <file src=""G:\trash\foo\*.*"" target="".\"" />
  </files>
</package>";

        Because of = () => _package = NuGetHelper.getNuspecProperties(Nuspec);

        It should_should_read__the_version = () => _package.Version.ShouldEqual("0.0.1-alpha");
        It should_should_read_the_id = () => _package.Id.ShouldEqual("foo");
    }


    public class when_reading_nuspec_values_with_no_nuspace_namespace
    {
        //static IEnumerable<NuGetHelper.NuSpecPackage> _releases;
        private static NuGetHelper.NuSpecPackage _package = null;
        const string Nuspec = @"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>foo</id>
    <version>0.0.1-alpha</version>
    <authors>ashic</authors>
    <owners>ashic</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>the foo thing</description>
    <copyright>Copyright 2015</copyright>
    <title>foo</title>
    <tags>ci msdeploy</tags>
  </metadata>
  <files>
    <file src=""G:\trash\foo\*.*"" target="".\"" />
  </files>
</package>";

        Because of = () => _package = NuGetHelper.getNuspecProperties(Nuspec);

        It should_should_read__the_version = () => _package.Version.ShouldEqual("0.0.1-alpha");
        It should_should_read_the_id = () => _package.Id.ShouldEqual("foo");
    }
}
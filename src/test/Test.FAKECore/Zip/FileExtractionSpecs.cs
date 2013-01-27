using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.Zip
{
    public class when_extracting_single_file_from_nupkg_file
    {
        static string _unzippedFileAsString;
        Because of = () => _unzippedFileAsString = ZipHelper.UnzipSingleFileInMemory("SignalR.nuspec", Path.Combine(TestData.TestDataDir, "deployments/SignalR/active/SignalR.0.4.0.nupkg"));

        It should_contain_xml =
            () => _unzippedFileAsString.ShouldStartWith("<?xml version=");
    }
}
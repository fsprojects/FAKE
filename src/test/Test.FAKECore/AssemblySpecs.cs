using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_getting_assembly_version_string
    {
        It should_return_valid_version_number_in_expected_form = () =>
        {
            var currentAssemblyVersion = Assembly.GetCallingAssembly().GetName().Version;
            var currentAssemblyFile = Assembly.GetCallingAssembly().Location;
            
            var fakeVersion = Fake.VersionHelper.GetAssemblyVersionString(currentAssemblyFile);

            fakeVersion.ShouldEqual(currentAssemblyVersion.Major + "." +
                                    currentAssemblyVersion.Minor + "." +
                                    currentAssemblyVersion.Build + "." +
                                    currentAssemblyVersion.Revision);
        };

        It should_return_valid_version_number = () =>
        {
            var currentAssemblyVersion = Assembly.GetCallingAssembly().GetName().Version;
            var currentAssemblyFile = Assembly.GetCallingAssembly().Location;

            var fakeVersion = Fake.VersionHelper.GetAssemblyVersionString(currentAssemblyFile);

            fakeVersion.ShouldEqual(currentAssemblyVersion.ToString());
        };
    }

    public class when_getting_assembly_version
    {
        It should_return_valid_version = () =>
        {
            var currentAssemblyVersion = Assembly.GetCallingAssembly().GetName().Version;
            var currentAssemblyFile = Assembly.GetCallingAssembly().Location;

            var fakeVersion = Fake.VersionHelper.GetAssemblyVersion(currentAssemblyFile);

            fakeVersion.ShouldEqual(currentAssemblyVersion);
        };
    }
}
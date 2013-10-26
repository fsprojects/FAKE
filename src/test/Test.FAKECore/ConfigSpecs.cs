using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.ConfigHandling
{
    public class when_modifying_app_settings
    {
        const string OriginalFile = "Small.txt";

        Because of = () =>  ConfigurationHelper.updateAppSetting("DatabaseName","MyDatabase",OriginalFile);

        It should_equal_the_target_text =
            () => File.ReadAllText(OriginalFile).ShouldContain("MyDatabase");
    }

    public class when_modifying_connection_strings
    {
        const string OriginalFile = "Small.txt";

        Because of = () => ConfigurationHelper.updateConnectionString("basic", "New Connection String", OriginalFile);

        It should_equal_the_target_text =
            () => File.ReadAllText(OriginalFile).ShouldContain("New Connection String");
    }
}
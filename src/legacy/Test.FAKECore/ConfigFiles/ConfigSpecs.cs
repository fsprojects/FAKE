using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.ConfigHandling
{
    public class when_modifying_app_settings_inside_of_configuration
    {
        const string OriginalFile = "ConfigFiles/SmallConfig.txt";
        static Guid Guid = new Guid();

        Because of = () =>  ConfigurationHelper.updateAppSetting("DatabaseName",Guid.ToString(),OriginalFile);

        It should_equal_the_target_text =
            () => File.ReadAllText(OriginalFile).ShouldContain(Guid.ToString());
    }

    public class when_modifying_app_settings_in_root
    {
        const string OriginalFile = "ConfigFiles/DynamicsNAVConfig.txt";
        static Guid Guid = new Guid();

        Because of = () => ConfigurationHelper.updateAppSetting("DatabaseName", Guid.ToString(), OriginalFile);

        It should_equal_the_target_text =
            () => File.ReadAllText(OriginalFile).ShouldContain(Guid.ToString());
    }

    public class when_modifying_connection_strings_inside_of_configuration
    {
        const string OriginalFile = "ConfigFiles/SmallConfig.txt";
        static Guid Guid = new Guid();

        Because of = () => ConfigurationHelper.updateConnectionString("basic", Guid.ToString(), OriginalFile);

        It should_equal_the_target_text =
            () => File.ReadAllText(OriginalFile).ShouldContain(Guid.ToString());
    }

    public class when_applying_xslt_in_configuration
    {
        const string OriginalFile = "ConfigFiles/SmallConfig.txt";
        const string TransformFile = "ConfigFiles/ConfigTransforms.xslt";

        Because of = () => ConfigurationHelper.applyXslOnConfig(TransformFile, OriginalFile);

        It should_equal_the_changed_database_text =
            () => File.ReadAllText(OriginalFile).ShouldContain("XsltDatabaseNameChanged");

        It should_equal_the_changed_connectionstring_text =
            () => File.ReadAllText(OriginalFile).ShouldContain("XsltDatabaseConnectionStringChanged");
    }
}
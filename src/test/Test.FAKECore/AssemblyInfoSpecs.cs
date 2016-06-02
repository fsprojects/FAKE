using System;
using System.Collections.Generic;
using System.IO;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Core;

namespace Test.FAKECore
{
    public class when_accessing_internals
    {
        It should_have_access_to_FAKE_internals =
            () => AssemblyInfoFile.getDependencies(new List<AssemblyInfoFile.Attribute>());
    }

    public class when_using_fsharp_task_with_default_config
    {
        It should_use_system_namespace_and_emit_a_version_module = () =>
        {
            string infoFile = Path.GetTempFileName();
            var attributes = new[]
            {
                AssemblyInfoFile.Attribute.Product("TestLib"),
                AssemblyInfoFile.Attribute.Version("1.0.0.0")
            };
            AssemblyInfoFile.CreateFSharpAssemblyInfo(infoFile, attributes);
            const string expected = "namespace System\r\nopen System.Reflection\r\n\r\n[<assembly: AssemblyProductAttribute(\"TestLib\")>]\r\n[<assembly: AssemblyVersionAttribute(\"1.0.0.0\")>]\r\ndo ()\r\n\r\nmodule internal AssemblyVersionInformation =\r\n    let [<Literal>] Version = \"1.0.0.0\"\r\n    let [<Literal>] InformationalVersion = \"1.0.0.0\"\r\n";

            File.ReadAllText(infoFile)
                .ShouldEqual(expected.Replace("\r\n", Environment.NewLine));
        };

        It update_attributes_should_update_attributes_in_fs_file = () =>
        {
            // Arrange. Create attribute both with and without "Attribute" at the end
            string infoFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".fs");
            const string original = "namespace System\r\nopen System.Reflection\r\n\r\n" +
                                     "[<assembly: AssemblyProduct(\"TestLib\")>]\r\n" +
                                     "[<assembly: AssemblyVersionAttribute(\"1.0.0.0\")>]\r\n";
            File.WriteAllText(infoFile, original.Replace("\r\n", Environment.NewLine));

            // Act
            var attributes = new[]
            {
                AssemblyInfoFile.Attribute.Product("TestLibNew"),
                AssemblyInfoFile.Attribute.Version("2.0.0.0")
            };
            AssemblyInfoFile.UpdateAttributes(infoFile, attributes);

            // Assert
            const string expected = "namespace System\r\nopen System.Reflection\r\n\r\n" +
                                    "[<assembly: AssemblyProduct(\"TestLibNew\")>]\r\n" +
                                    "[<assembly: AssemblyVersionAttribute(\"2.0.0.0\")>]\r\n";

            File.ReadAllText(infoFile)
                .ShouldEqual(expected.Replace("\r\n", Environment.NewLine));
        };

        It get_attribute_should_read_attribute_from_fs_file = () =>
        {
            // Arrange. Create attribute both with and without "Attribute" at the end
            string infoFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".fs");
            const string original = "namespace System\r\nopen System.Reflection\r\n\r\n" +
                                     "[<assembly: AssemblyProduct(\"TestLib\")>]\r\n" +
                                     "[<assembly: AssemblyVersionAttribute(\"1.0.0.0\")>]\r\n";
            File.WriteAllText(infoFile, original.Replace("\r\n", Environment.NewLine));

            // Act
            var productAttr = AssemblyInfoFile.GetAttribute("AssemblyProduct", infoFile).Value;
            var versionAttr = AssemblyInfoFile.GetAttribute("AssemblyVersion", infoFile).Value;

            // Assert
            productAttr.Value.ShouldEqual("\"TestLib\"");
            versionAttr.Value.ShouldEqual("\"1.0.0.0\"");
        };
    }

    public class when_using_fsharp_task_with_custom_config
    {
        It should_use_custom_namespace_and_not_emit_a_version_module = () =>
        {
            var customConfig = new AssemblyInfoFile.AssemblyInfoFileConfig(false, new FSharpOption<string>("Custom"));
            string infoFile = Path.GetTempFileName();
            var attributes = new[]
            {
                AssemblyInfoFile.Attribute.Product("TestLib"),
                AssemblyInfoFile.Attribute.Version("1.0.0.0")
            };
            AssemblyInfoFile.CreateFSharpAssemblyInfoWithConfig(infoFile, attributes, customConfig);
            const string expected = "namespace Custom\r\nopen System.Reflection\r\n\r\n[<assembly: AssemblyProductAttribute(\"TestLib\")>]\r\n[<assembly: AssemblyVersionAttribute(\"1.0.0.0\")>]\r\ndo ()\r\n\r\n";
            File.ReadAllText(infoFile)
                .ShouldEqual(expected.Replace("\r\n", Environment.NewLine));
        };
    }

    public class when_using_csharp_task_with_default_config
    {
        It should_use_system_namespace_and_emit_a_version_module = () =>
        {
            string infoFile = Path.GetTempFileName();
            var attributes = new[]
            {
                AssemblyInfoFile.Attribute.Product("TestLib"),
                AssemblyInfoFile.Attribute.Version("1.0.0.0")
            };
            AssemblyInfoFile.CreateCSharpAssemblyInfo(infoFile, attributes);
            const string expected = "// <auto-generated/>\r\nusing System.Reflection;\r\n\r\n[assembly: AssemblyProductAttribute(\"TestLib\")]\r\n[assembly: AssemblyVersionAttribute(\"1.0.0.0\")]\r\nnamespace System {\r\n    internal static class AssemblyVersionInformation {\r\n        internal const string Version = \"1.0.0.0\";\r\n        internal const string InformationalVersion = \"1.0.0.0\";\r\n    }\r\n}\r\n";

            File.ReadAllText(infoFile)
                .ShouldEqual(expected.Replace("\r\n", Environment.NewLine));
        };

        It update_attributes_should_update_attributes_in_cs_file = () =>
        {
            // Arrange. Create attribute both with and without "Attribute" at the end
            string infoFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cs");
            const string original = "// <auto-generated/>\r\nusing System.Reflection;\r\n\r\n" +
                                    "[assembly: AssemblyProduct(\"TestLib\")]\r\n" +
                                    "[assembly: AssemblyVersionAttribute(\"1.0.0.0\")]\r\n";
            File.WriteAllText(infoFile, original.Replace("\r\n", Environment.NewLine));

            // Act
            var attributes = new[]
            {
                AssemblyInfoFile.Attribute.Product("TestLibNew"),
                AssemblyInfoFile.Attribute.Version("2.0.0.0")
            };
            AssemblyInfoFile.UpdateAttributes(infoFile, attributes);

            // Assert
            const string expected = "// <auto-generated/>\r\nusing System.Reflection;\r\n\r\n" +
                                    "[assembly: AssemblyProduct(\"TestLibNew\")]\r\n" +
                                    "[assembly: AssemblyVersionAttribute(\"2.0.0.0\")]\r\n";

            File.ReadAllText(infoFile)
                .ShouldEqual(expected.Replace("\r\n", Environment.NewLine));
        };

        It get_attribute_should_read_attribute_from_cs_file = () =>
        {
            // Arrange. Create attribute both with and without "Attribute" at the end
            string infoFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cs");
            const string original = "// <auto-generated/>\r\nusing System.Reflection;\r\n\r\n" +
                                    "[assembly: AssemblyProduct(\"TestLib\")]\r\n" +
                                    "[assembly: AssemblyVersionAttribute(\"1.0.0.0\")]\r\n";
            File.WriteAllText(infoFile, original.Replace("\r\n", Environment.NewLine));

            // Act
            var productAttr = AssemblyInfoFile.GetAttribute("AssemblyProduct", infoFile).Value;
            var versionAttr = AssemblyInfoFile.GetAttribute("AssemblyVersion", infoFile).Value;

            // Assert
            productAttr.Value.ShouldEqual("\"TestLib\"");
            versionAttr.Value.ShouldEqual("\"1.0.0.0\"");
        };
    }


    public class when_using_cppcli_task_with_default_config
    {
        It should_emit_valid_syntax = () =>
        {
            string infoFile = Path.GetTempFileName();
            var attributes = new[]
            {
                AssemblyInfoFile.Attribute.Product("TestLib"),
                AssemblyInfoFile.Attribute.Version("1.0.0.0")
            };
            AssemblyInfoFile.CreateCppCliAssemblyInfo(infoFile, attributes);
            const string expected = "// <auto-generated/>\r\nusing namespace System::Reflection;\r\n\r\n[assembly:AssemblyProductAttribute(\"TestLib\")];\r\n[assembly:AssemblyVersionAttribute(\"1.0.0.0\")];\r\n";

            File.ReadAllText(infoFile)
                .ShouldEqual(expected.Replace("\r\n", Environment.NewLine));
        };

        It update_attributes_should_update_attributes_in_cpp_file = () =>
        {
            // Arrange. Create attribute both with and without "Attribute" at the end
            string infoFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cpp");
            const string original = "// <auto-generated/>\r\nusing namespace System::Reflection;\r\n\r\n" +
                                    "[assembly:AssemblyProduct(\"TestLib\")];\r\n" +
                                    "[assembly:AssemblyVersionAttribute(\"1.0.0.0\")];\r\n";
            File.WriteAllText(infoFile, original.Replace("\r\n", Environment.NewLine));

            // Act
            var attributes = new[]
            {
                AssemblyInfoFile.Attribute.Product("TestLibNew"),
                AssemblyInfoFile.Attribute.Version("2.0.0.0")
            };
            AssemblyInfoFile.UpdateAttributes(infoFile, attributes);

            // Assert
            const string expected = "// <auto-generated/>\r\nusing namespace System::Reflection;\r\n\r\n" +
                                    "[assembly:AssemblyProduct(\"TestLibNew\")];\r\n" +
                                    "[assembly:AssemblyVersionAttribute(\"2.0.0.0\")];\r\n";

            File.ReadAllText(infoFile)
                .ShouldEqual(expected.Replace("\r\n", Environment.NewLine));
        };

        It get_attribute_should_read_attribute_from_cpp_file = () =>
        {
            // Arrange. Create attribute both with and without "Attribute" at the end
            string infoFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cpp");
            const string original = "// <auto-generated/>\r\nusing namespace System::Reflection;\r\n\r\n" +
                                    "[assembly:AssemblyProduct(\"TestLib\")];\r\n" +
                                    "[assembly:AssemblyVersionAttribute(\"1.0.0.0\")];\r\n";
            File.WriteAllText(infoFile, original.Replace("\r\n", Environment.NewLine));

            // Act
            var productAttr = AssemblyInfoFile.GetAttribute("AssemblyProduct", infoFile).Value;
            var versionAttr = AssemblyInfoFile.GetAttribute("AssemblyVersion", infoFile).Value;

            // Assert
            productAttr.Value.ShouldEqual("\"TestLib\"");
            versionAttr.Value.ShouldEqual("\"1.0.0.0\"");
        };
    }


    public class when_using_vb_task_with_default_config
    {
        It should_emit_valid_syntax = () =>
        {
            string infoFile = Path.GetTempFileName();
            var attributes = new[]
            {
                AssemblyInfoFile.Attribute.Product("TestLib"),
                AssemblyInfoFile.Attribute.Version("1.0.0.0")
            };
            AssemblyInfoFile.CreateVisualBasicAssemblyInfo(infoFile, attributes);
            const string expected = "' <auto-generated/>\r\nImports System.Reflection\r\n\r\n<assembly: AssemblyProductAttribute(\"TestLib\")>\r\n<assembly: AssemblyVersionAttribute(\"1.0.0.0\")>\r\nFriend NotInheritable Class AssemblyVersionInformation\r\n    Friend Const Version As String = \"1.0.0.0\"\r\n    Friend Const InformationalVersion As String = \"1.0.0.0\"\r\nEnd Class\r\n";

            File.ReadAllText(infoFile)
                .ShouldEqual(expected.Replace("\r\n", Environment.NewLine));
        };

        It update_attributes_should_update_attributes_in_vb_file = () =>
        {
            // Arrange. Create attribute both with and without "Attribute" at the end
            string infoFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".vb");
            const string original = "' <auto-generated/>\r\nImports System.Reflection\r\n\r\n" +
                                    "<assembly: AssemblyProduct(\"TestLib\")>\r\n" +
                                    "<Assembly: AssemblyCompany(\"TestCompany\")>\r\n" +
                                    "<assembly: AssemblyVersionAttribute(\"1.0.0.0\")>\r\n" +
                                    "<Assembly: ComVisibleAttribute(false)>";
            File.WriteAllText(infoFile, original.Replace("\r\n", Environment.NewLine));

            // Act
            var attributes = new[]
            {
                AssemblyInfoFile.Attribute.Product("TestLibNew"),
                AssemblyInfoFile.Attribute.Company("TestCompanyNew"),
                AssemblyInfoFile.Attribute.Version("2.0.0.0")
            };
            AssemblyInfoFile.UpdateAttributes(infoFile, attributes);

            // Assert
            const string expected = "' <auto-generated/>\r\nImports System.Reflection\r\n\r\n" +
                                    "<assembly: AssemblyProduct(\"TestLibNew\")>\r\n" +
                                    "<Assembly: AssemblyCompany(\"TestCompanyNew\")>\r\n" +
                                    "<assembly: AssemblyVersionAttribute(\"2.0.0.0\")>\r\n" +
                                    "<Assembly: ComVisibleAttribute(false)>";

            File.ReadAllText(infoFile)
                .ShouldEqual(expected.Replace("\r\n", Environment.NewLine));
        };

        It get_attribute_should_read_attribute_from_vb_file = () =>
        {
            // Arrange. Create attribute both with and without "Attribute" at the end, and also
            // case-insensitive attributes
            string infoFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".vb");
            const string original = "' <auto-generated/>\r\nImports System.Reflection\r\n\r\n" +
                                    "<assembly: AssemblyProduct(\"TestLib\")>\r\n" +
                                    "<Assembly: AssemblyCompany(\"TestCompany\")>\r\n" +
                                    "<assembly: AssemblyVersionAttribute(\"1.0.0.0\")>\r\n" +
                                    "<Assembly: ComVisibleAttribute(false)>";
            File.WriteAllText(infoFile, original.Replace("\r\n", Environment.NewLine));

            // Act
            var productAttr = AssemblyInfoFile.GetAttribute("AssemblyProduct", infoFile).Value;
            var companyAttr = AssemblyInfoFile.GetAttribute("AssemblyCompany", infoFile).Value;
            var versionAttr = AssemblyInfoFile.GetAttribute("AssemblyVersion", infoFile).Value;
            var comVisibleAttr = AssemblyInfoFile.GetAttribute("ComVisible", infoFile).Value;

            // Assert
            productAttr.Value.ShouldEqual("\"TestLib\"");
            companyAttr.Value.ShouldEqual("\"TestCompany\"");
            versionAttr.Value.ShouldEqual("\"1.0.0.0\"");
            comVisibleAttr.Value.ShouldEqual("false");
        };
    }
}

using System.Collections.Generic;
using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_accessing_internals
    {
        It should_have_access_to_FAKE_internals = () => AssemblyInfoFile.getDependencies(new List<AssemblyInfoFile.Attribute>());
    }

    public class when_use_fsharp_task_with_default_config
    { 
      It should_use_system_namespace_and_emit_a_verison_module = () => 
      {
        var infoFile = Path.GetTempFileName();
        var attributes = new []
          {
            AssemblyInfoFile.Attribute.Product("TestLib"),
            AssemblyInfoFile.Attribute.Version("1.0.0.0")
          };
        AssemblyInfoFile.CreateFSharpAssemblyInfo(infoFile, attributes);
        File.ReadAllText(infoFile)
            .ShouldEqual("namespace System\r\nopen System.Reflection\r\n\r\n[<assembly: AssemblyProductAttribute(\"TestLib\")>]\r\n[<assembly: AssemblyVersionAttribute(\"1.0.0.0\")>]\r\ndo ()\r\n\r\nmodule internal AssemblyVersionInformation =\r\n    let [<Literal>] Version = \"1.0.0.0\"\r\n");
      };
    }

    public class when_use_fsharp_task_with_custom_config
    { 
      It should_use_custom_namespace_and_not_emit_a_version_module = () => 
      { 
        var customConfig = new AssemblyInfoFile.AssemblyInfoFileConfig(false,"Custom");
        var infoFile = Path.GetTempFileName();
        var attributes = new []
          {
            AssemblyInfoFile.Attribute.Product("TestLib"),
            AssemblyInfoFile.Attribute.Version("1.0.0.0")
          };
        AssemblyInfoFile.CreateFSharpAssemblyInfoWithConfig(infoFile, attributes, customConfig);
        File.ReadAllText(infoFile)
            .ShouldEqual("namespace Custom\r\nopen System.Reflection\r\n\r\n[<assembly: AssemblyProductAttribute(\"TestLib\")>]\r\n[<assembly: AssemblyVersionAttribute(\"1.0.0.0\")>]\r\ndo ()\r\n\r\n");
      };
    }
}
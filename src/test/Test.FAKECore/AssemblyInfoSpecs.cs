using System.Collections.Generic;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_accessing_internals
    {
        It should_have_access_to_FAKE_internals = () => AssemblyInfoFile.getDependencies(new List<AssemblyInfoFile.Attribute>());
    }
}
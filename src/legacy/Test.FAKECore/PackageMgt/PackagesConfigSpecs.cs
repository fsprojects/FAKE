using System;
using System.IO;
using System.Linq;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Collections;

namespace Test.FAKECore.PackageMgt
{
    public class when_parsing_packages_config
    {
        public static FSharpList<Tuple<string, string>> Dependencies;

        private Because of = () => Dependencies = NuGetHelper.getDependencies(Path.Combine(TestData.TestDataDir, "fake.packages.config"));

        It should_containt_3_packages =
            () => Dependencies.Count().ShouldEqual(3);

        It should_contain_the_package_id =
            () => Dependencies.First().Item1.ShouldEqual("Microsoft.Web.Xdt");

        It should_contain_the_package_version =
            () => Dependencies.First().Item2.ShouldEqual("1.0.0");
    }
   
}

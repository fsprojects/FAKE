using System.Linq;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_combining_paths
    {
        It should_act_as_path_combine =
            () => {
                var testValues  = new []{
                    new []{"c:\\test", "dir\\asdf"},
                    new []{"\\test", "dir\\asdf"},
                    new []{"test", "dir\\asdf"},
                    new []{"/test", "dir/asdf"},
                    new []{"/test", "/dir/asdf"},
                    new []{"c:\\test", "d:\\dir\\asdf"},
                    new []{"/asdf/asdf/asdf/", "/asdf/asdf"},
                    new []{"c:\\test", "\\\\dir\\asdf"},
                    new []{"c:\\test", "/dir\\asdf"},
                };

                foreach(var item in testValues)
                {
                    EnvironmentHelper.combinePaths(item[0], item[1])
                        .ShouldEqual(System.IO.Path.Combine(item[0], item[1]));
                }
            };
    }
}

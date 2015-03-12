using System.IO;
using System.Collections.Generic;
using System.Linq;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_combining_paths
    {
        It should_strip_leading_slashes_when_using_combinePaths =
            () => 
            {
                var testValues = new List<string[]> {
                    new[]{"/test/path", "/of/the/thing", "/test/path" + Path.DirectorySeparatorChar + "of/the/thing"},
                    new[]{"/test/path", "of/the/thing", "/test/path" + Path.DirectorySeparatorChar + "of/the/thing"},
                };

                if (Path.VolumeSeparatorChar != Path.DirectorySeparatorChar)
                {
                    var path2 = "X" + Path.VolumeSeparatorChar + "/test";
                    testValues.Add(new[]{"/test/path", path2, path2});
                }

                foreach (var item in testValues)
                {
                    EnvironmentHelper.combinePaths(item[0], item[1]).ShouldEqual(item[2]);
                }
            };

        It should_work_like_path_dot_combine_when_using_combinePathsNoTrim =
            () => 
            {
                var testValues = new List<string[]> {
                    new[]{"/test/path", "/of/the/thing", "/of/the/thing"},
                    new[]{"/test/path", "of/the/thing", "/test/path" + Path.DirectorySeparatorChar + "of/the/thing"},
                };

                if (Path.VolumeSeparatorChar != Path.DirectorySeparatorChar)
                {
                    var path2 = "X" + Path.VolumeSeparatorChar + "/test";
                    testValues.Add(new[]{"/test/path", path2, path2});
                }

                foreach (var item in testValues)
                {
                    EnvironmentHelper.combinePathsNoTrim(item[0], item[1]).ShouldEqual(item[2]);
                }
            };
    }
}

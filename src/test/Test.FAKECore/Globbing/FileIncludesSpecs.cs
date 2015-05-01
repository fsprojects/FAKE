using System.Linq;
using Fake;
using Machine.Specifications;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore.Globbing
{
    public class when_matching_files_to_patterns_using_filesystem : BaseFunctions
    {
        static void IsMatch(string include, string[] files)
        {
            var fi = FileSystem.Include(include).SetBaseDirectory("/root");
            foreach (var file in files)
            {
                fi.IsMatch(file).ShouldBeTrue();
            }
        }

        static void IsMatchWithExclude(string include, string exclude, string[] files)
        {
            var fi = FileSystem.Include(include).ButNot(exclude).SetBaseDirectory("/root");
            foreach (var file in files)
            {
                fi.IsMatch(file).ShouldBeTrue();
            }
        }

        static void IsNotMatch(string include, string[] files)
        {
            var fi = FileSystem.Include(include).SetBaseDirectory("/root");
            foreach (var file in files)
            {
                fi.IsMatch(file).ShouldBeFalse();
            }
        }

        static void IsNotMatchWithExclude(string include, string exclude, string[] files)
        {
            var fi = FileSystem.Include(include).ButNot(exclude).SetBaseDirectory("/root");
            foreach (var file in files)
            {
                fi.IsMatch(file).ShouldBeFalse();
            }
        }

        It should_match =
            () => IsMatch("foo.bar", new[] { "/root/foo.bar" });

        It should_not_match =
            () => IsNotMatch("foo.bar", new[] { "foo.bar", "/other/foo.bar" });

        It should_match_absolute =
            () => IsMatch("/other/**/*.*", new[] { "/other/foo.bar", "/other/sub/foo.bar" });

        It should__not_match_absolute =
            () => IsNotMatch("/other/**/*.*", new[] { "foo.bar", "sub/foo.bar" });
    }
}

using System.Linq;
using Fake;
using Machine.Specifications;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore.Globbing
{
    public class when_matching_files_to_patterns : BaseFunctions
    {
        static void IsMatch(string pattern, string[] files)
        {
            foreach (var file in files)
            {
                Fake.Globbing.isMatch(pattern, file).ShouldBeTrue();
            }
        }

        static void IsNotMatch(string pattern, string[] files)
        {
            foreach (var file in files)
            {
                Fake.Globbing.isMatch(pattern, file).ShouldBeFalse();
            }
        }

        private It should_exact_match =
            () => IsMatch("foo.bar", new[] { "foo.bar" });

        It should_not_match =
            () => IsNotMatch("foo.bar", new[] { "baz.bar", "baz/foo.bar" });

        It should_match_any_in_root =
            () => IsMatch("*.*", new[] { "foo.bar", "foo.baz" });

        It should_not_match_in_sub_dir =
            () => IsNotMatch("*.*", new[] { "foo/bar/baz.bill", "foo/bar.baz" });

        It should_match_filename =
            () => IsMatch("*.bar", new[] { "foo.bar", "baz.bar" });

        It should_not_match_filename =
            () => IsNotMatch("*.bar", new[] { "foo.baz", "baz.foo" });

        It should_match_subdir =
            () => IsMatch("foo/*.bar", new[] { "foo/baz.bar", "foo/foo.bar" });

        It should_not_match_different_subdir =
            () => IsNotMatch("foo/*.bar", new[] { "baz/foo.bar", "foo/bar/baz.bar" });

        It should_match_any =
            () => IsMatch("**/*.*", new[] {
                "foo", 
                "foo.bar", 
                "foo/baz", 
                "foo/baz.bill", 
                "foo/baz/x.bill"
            });

        It should_match_any_with_extension =
            () => IsMatch("**/*.bar", new[] {
                "foo.bar", 
                "foo/baz.bar", 
                "foo/bar/bill.bar"
            });

        It should_not_match_any_with_wrong_extension =
            () => IsNotMatch("**/*.bar", new[] {
                "foo.baz", 
                "foo/baz.baz", 
                "foo/bar/bill.baz"
            });

        It should_match_any_subdir_with_extension =
            () => IsMatch("foo/**/*.bar", new[] {
                "foo/baz.bar", 
                "foo/baz/bill.bar", 
            });

        It should_not_match_any_in_wrong_subdir_with_extension =
            () => IsNotMatch("foo/**/*.bar", new[] {
                "bill/baz.bar", 
                "baz/foo/bill.bar", 
            });

        It should_match_particular_subdir =
            () => IsMatch("**/subdir/more/*.bar", new[] {
                "subdir/more/foo.bar",
                "root/subdir/more/foo.bar",
                "root/another/subdir/more/foo.bar"
            });

        It should_not_match_particular_subdir =
            () => IsNotMatch("**/subdir/more/*.bar", new[] {
                "wrong/foo.bar",
                "foo.bar",
                "subdir/more/wrong/foo.bar"
            });

        It should_match_particular_subdir_then_recursive =
            () => IsMatch("**/subdir/more/**/*.bar", new[] {
                "root/subdir/more/foo.bar",
                "root/subdir/more/another/foo.bar",
                "root/another/subdir/more/foo.bar"
            });

        It should_match_any_file_type =
            () => IsMatch("/some/random/path/**/Properties/*", new[] {
                "/some/random/path/somewhere/Properties/AssemblyInfo.cs"
            });

    }
}

using System.Linq;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Collections;

namespace Test.FAKECore
{
    public class when_calculating_directories_to_watch
    {
        It should_watch_multiple_directories =
            () =>
            {
                var includes = ListModule.OfArray(new[] { @"test1\bin\*.dll", @"test2\bin\*.dll", });
                var fileIncludes = new FileSystem.FileIncludes(@"C:\Project", includes, ListModule.Empty<string>());

                var dirsToWatch = ChangeWatcher.calcDirsToWatch(fileIncludes);

                dirsToWatch.Length.ShouldEqual(2);
                dirsToWatch.ShouldContain(Fake.EnvironmentHelper.normalizePath(@"C:\Project\test1\bin"));
                dirsToWatch.ShouldContain(Fake.EnvironmentHelper.normalizePath(@"C:\Project\test2\bin"));
            };

        It should_only_take_the_most_root_path_when_multiple_directories_share_a_root =
            () =>
            {
                var includes = ListModule.OfArray(new[] { @"tests\**\test1\bin\*.dll", @"tests\test2\bin\*.dll", });
                var fileIncludes = new FileSystem.FileIncludes(@"C:\Project", includes, ListModule.Empty<string>());

                var dirsToWatch = ChangeWatcher.calcDirsToWatch(fileIncludes);

                dirsToWatch.Length.ShouldEqual(1);
                dirsToWatch.ShouldContain(Fake.EnvironmentHelper.normalizePath(@"C:\Project\tests"));
            };
    }
}

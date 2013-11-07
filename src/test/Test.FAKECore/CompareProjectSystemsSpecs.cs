using System;
using Fake.MsBuild;
using Machine.Specifications;
using Microsoft.FSharp.Collections;

namespace Test.FAKECore
{
    public class when_searching_for_missing_files
    {
        const string Project = "ProjectTestFiles/FakeLib.proj";
        const string Project2 = "ProjectTestFiles/FakeLib2.proj";
        static Tuple<FSharpSet<string>, FSharpSet<string>> _missing;

        Because of = () => _missing = ProjectSystem.findMissingFiles(Project, Project2);

        It should_detect_missing_files_in_project1 = () =>
        {
            _missing.Item2.Count.ShouldEqual(2);
            _missing.Item2.ShouldContain("Git\\Merge.fs");
            _missing.Item2.ShouldContain("Git\\Stash.fs");
        };

        It should_detect_missing_files_in_project2 = () =>
        {
            _missing.Item1.Count.ShouldEqual(1);
            _missing.Item1.ShouldContain("Messages.fs");
        };
    }
}
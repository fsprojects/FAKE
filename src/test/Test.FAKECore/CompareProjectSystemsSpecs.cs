using System;
using System.Collections.Generic;
using System.Linq;
using Fake.MsBuild;
using Machine.Specifications;
using Microsoft.FSharp.Collections;

namespace Test.FAKECore
{
    public class when_searching_for_missing_files_in_project2
    {
        const string Project = "ProjectTestFiles/FakeLib.proj";
        const string Project2 = "ProjectTestFiles/FakeLib2.proj";
        static Tuple<string, FSharpSet<string>> _missing;

        Because of = () => _missing = ProjectSystem.findMissingFiles(Project, new List<string> {Project2}).First();

        It should_detect_missing_files_in_project2 = () =>
        {
            _missing.Item1.SequenceEqual(Project2);
            _missing.Item2.Count.ShouldEqual(2);
            _missing.Item2.ShouldContain("Git\\Merge.fs");
            _missing.Item2.ShouldContain("Git\\Stash.fs");
        };
    }

    public class when_comparing_project2_with_project1
    {
        const string Project = "ProjectTestFiles/FakeLib.proj";
        const string Project2 = "ProjectTestFiles/FakeLib2.proj";
        static Exception _exn;

        Because of =
            () => _exn = Catch.Exception(() => ProjectSystem.compareProjects(Project, new List<string> {Project2}));

        It should_fire_useful_exception = () =>
        {
            _exn.Message.ShouldContain("ProjectTestFiles/FakeLib2.proj");
            _exn.Message.ShouldContain("Git\\Merge.fs");
            _exn.Message.ShouldContain("Git\\Stash.fs");
        };
    }

    public class when_searching_for_missing_files_in_project1
    {
        const string Project = "ProjectTestFiles/FakeLib.proj";
        const string Project2 = "ProjectTestFiles/FakeLib2.proj";
        static Tuple<string, FSharpSet<string>> _missing;

        Because of = () => _missing = ProjectSystem.findMissingFiles(Project2, new List<string> {Project}).First();

        It should_detect_missing_files_in_project1 = () =>
        {
            _missing.Item1.SequenceEqual(Project);
            _missing.Item2.Count.ShouldEqual(1);
            _missing.Item2.ShouldContain("Messages.fs");
        };
    }
}
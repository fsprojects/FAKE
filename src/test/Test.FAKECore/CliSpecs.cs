using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_parsing_the_fake_cli
    {
        It should_parse_solo_positional_script =
            () => Cli.parsePositionalArgs(new string[] { "fake.exe", "script.fsx" }).Script.Value.ShouldEqual("script.fsx");

        It should_parse_positional_script_with_target =
            () => Cli.parsePositionalArgs(new string[] { "fake.exe", "script.fsx", "clean" }).Script.Value.ShouldEqual("script.fsx");

        It should_parse_solo_positional_target =
            () => Cli.parsePositionalArgs(new string[] { "fake.exe", "clean" }).Target.Value.ShouldEqual("clean");

        It should_parse_positional_target_with_script =
            () => Cli.parsePositionalArgs(new string[] { "fake.exe", "script.fsx", "clean" }).Target.Value.ShouldEqual("clean");

        It should_parse_rest_of_args =
            () => Cli.parsePositionalArgs(new string[] { "fake.exe", "script.fsx", "clean", "-a", "-b", "-c" }).Rest.Length.ShouldEqual(4);

        It should_parse_rest_of_args_when_no_positional =
            () => Cli.parsePositionalArgs(new string[] { "fake.exe", "-a", "-b", "-c" }).Rest.Length.ShouldEqual(4);
    }
}

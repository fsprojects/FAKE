using System.IO;
using System.Collections.Generic;
using System.Linq;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_parsing_command_line_params
    {
        It should_parse_value_sets =
            () =>
            {
                System.Action<string[]> check = ((string[] args) => {
                    var parametters = CommandLineHelper.parseCLIParams(args);
                    var fooVal = CommandLineHelper.explicitGetCLIParam(parametters, "foo");
                    var bazVal = CommandLineHelper.explicitGetCLIParam(parametters, "baz");
                    var noneVal = CommandLineHelper.explicitGetCLIParam(parametters, "none");
                    var flagVal = CommandLineHelper.explicitGetCLIParam(parametters, "flag");
                    var trueflagVal = CommandLineHelper.explicitGetCLIFlag(parametters, "trueflag");
                    var falseflagVal = CommandLineHelper.explicitGetCLIFlag(parametters, "falseflag");
                    fooVal.ShouldEqual(Microsoft.FSharp.Core.FSharpOption<string>.Some("bar"));
                    bazVal.ShouldEqual(Microsoft.FSharp.Core.FSharpOption<string>.Some("bill"));
                    noneVal.ShouldEqual(Microsoft.FSharp.Core.FSharpOption<string>.None);
                    flagVal.ShouldEqual(Microsoft.FSharp.Core.FSharpOption<string>.Some(""));
                    trueflagVal.ShouldEqual(Microsoft.FSharp.Core.FSharpOption<bool>.Some(true));
                    falseflagVal.ShouldEqual(Microsoft.FSharp.Core.FSharpOption<bool>.Some(false));
                });

                check(new[] { "-foo", "bar", "-flag", "-trueflag", "true", "-falseflag", "false", "-baz", "bill" });
                check(new[] { "--foo", "bar", "-flag", "-trueflag", "true", "-falseflag", "false", "-baz", "bill" });
                check(new[] { "/foo", "bar", "-flag", "-trueflag", "true", "-falseflag", "false", "-baz", "bill" });
                //check(new[]{"-foo=bar", "-flag", "-baz", "bill"});
                //check(new[]{"--foo=bar", "-flag", "-baz", "bill"});
                //check(new[]{"/foo=bar", "-flag", "-baz", "bill"});
                //check(new[]{"/foo=\"bar\"", "-flag", "-baz", "bill"});
                //check(new[] { "/foo='bar'", "-flag", "-baz", "bill" });
            };

        
    }
}

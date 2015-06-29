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
                    var fooVal = CommandLineHelper.explicitReadCLIParam(parametters, "foo");
                    var bazVal = CommandLineHelper.explicitReadCLIParam(parametters, "baz");
                    var noneVal = CommandLineHelper.explicitReadCLIParam(parametters, "none");
                    var flagVal = CommandLineHelper.explicitReadCLIParam(parametters, "flag");
                    var trueflagVal = CommandLineHelper.explicitReadCLIFlag(parametters, "trueflag");
                    var falseflagVal = CommandLineHelper.explicitReadCLIFlag(parametters, "falseflag");
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

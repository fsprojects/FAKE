#pragma warning disable 612, 618

﻿using System;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Core;

namespace Test.FAKECore
{
    public class XUnitHelperSpecs
    {
        It args_should_not_include_traits = () => BuildXUnitArgs(XUnitHelper.emptyTrait, XUnitHelper.emptyTrait).ShouldNotContain("trait");
        It args_should_include_one_trait = () => BuildXUnitArgs(Trait("name", "value"),  XUnitHelper.emptyTrait).ShouldContain(@" /trait ""name=value""");
        It args_should_include_two_traits = () => BuildXUnitArgs(Trait("name", "value1,value2"), XUnitHelper.emptyTrait).ShouldContain(@" /trait ""name=value1"" /trait ""name=value2""");
        It args_should_exclude_one_trait = () => BuildXUnitArgs(XUnitHelper.emptyTrait, Trait("name", "value")).ShouldContain(@"/-trait ""name=value""");
        It args_should_exclude_two_traits = () => BuildXUnitArgs(XUnitHelper.emptyTrait, Trait("name", "value1,value2")).ShouldContain(@" /-trait ""name=value1"" /-trait ""name=value2""");
        It args_should_include_and_exclude_traits = () => BuildXUnitArgs(Trait("name", "value1"), Trait("name", "value2")).ShouldContain(@" /trait ""name=value1"" /-trait ""name=value2""");
        It args_should_include_and_exclude_multiple_traits = () => BuildXUnitArgs(Trait("name", "value1,value2"), Trait("name", "value3")).ShouldContain(@" /trait ""name=value1"" /trait ""name=value2"" /-trait ""name=value3""");

        private static FSharpOption<Tuple<string, string>> Trait(string name, string values)
        {
            return new FSharpOption<Tuple<string, string>>(new Tuple<string, string>(name,values));
        }

        private static string BuildXUnitArgs(FSharpOption<Tuple<string, string>> includeTrait, FSharpOption<Tuple<string, string>> excludeTrait)
        {
            return XUnitHelper.buildXUnitArgs(new XUnitHelper.XUnitParams("","",false,false,false,"",false,false,TimeSpan.MinValue,"",UnitTestCommon.TestRunnerErrorLevel.Error,includeTrait,excludeTrait), "test.dll");
        }
    }
}

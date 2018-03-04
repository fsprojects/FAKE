#pragma warning disable 612, 618

﻿using System;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Core;

namespace Test.FAKECore
{
    public class XUnit2HelperSpecs
    {
        It args_should_not_include_traits = () => BuildXUnit2Args(XUnit2Helper.empty2Trait, XUnit2Helper.empty2Trait).ShouldNotContain("trait");
        It args_should_include_one_trait = () => BuildXUnit2Args(Trait("name", "value"),  XUnit2Helper.empty2Trait).ShouldContain(@" -trait ""name=value""");
        It args_should_include_two_traits = () => BuildXUnit2Args(Trait("name", "value1,value2"), XUnit2Helper.empty2Trait).ShouldContain(@" -trait ""name=value1"" -trait ""name=value2""");
        It args_should_exclude_one_trait = () => BuildXUnit2Args(XUnit2Helper.empty2Trait, Trait("name", "value")).ShouldContain(@"-notrait ""name=value""");
        It args_should_exclude_two_traits = () => BuildXUnit2Args(XUnit2Helper.empty2Trait, Trait("name", "value1,value2")).ShouldContain(@" -notrait ""name=value1"" -notrait ""name=value2""");
        It args_should_contain_paraller = () => BuildXUnit2Args(XUnit2Helper.empty2Trait, XUnit2Helper.empty2Trait).ShouldContain(@"-parallel");

        private static FSharpOption<Tuple<string, string>> Trait(string name, string values)
        {
            return new FSharpOption<Tuple<string, string>>(new Tuple<string, string>(name,values));
        }

        private static string BuildXUnit2Args(FSharpOption<Tuple<string, string>> includeTrait, FSharpOption<Tuple<string, string>> excludeTrait)
        {
            var parameters = new XUnit2Helper.XUnit2Params("", "", XUnit2Helper.ParallelOption.None, 0, false, false,
                true, false, false,null, TimeSpan.FromMinutes(5), UnitTestCommon.TestRunnerErrorLevel.Error, includeTrait, excludeTrait, false, false, true, "");
            return XUnit2Helper.buildXUnit2Args(parameters, "test.dll");
        }
    }
}

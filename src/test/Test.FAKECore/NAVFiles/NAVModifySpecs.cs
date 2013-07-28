using System.Collections.Generic;
using System.IO;
using Fake;
using Machine.Specifications;
using System;

namespace Test.FAKECore.NAVFiles
{
    public class CanRemoveModifyFlag
    {
        static string _navisionObject;
        static string _expectedObject;
        static string _result;

        Establish context = () =>
        {
            string result = @"NAVFiles\Codeunit_1.txt";
            string original = @"NAVFiles\Codeunit_1_Modified.txt";

            _navisionObject = File.ReadAllText(original);
            _expectedObject = File.ReadAllText(result);
        };

        It should_remove_the_modified_flag = () => DynamicsNav.removeModifiedFlag(_navisionObject)
            .ShouldEqual(_expectedObject);
    }
}
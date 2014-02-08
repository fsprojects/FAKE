using System.IO;
using Fake;
using Machine.Specifications;
using System;

namespace Test.FAKECore.NAVFiles
{
    public class CanReplaceDate
    {
        static string _navisionObject;
        static string _expectedObject;
        static string _result;

        Establish context = () =>
        {
            const string result = @"NAVFiles/Codeunit_1_with_Date_changed.txt";
            const string original = @"NAVFiles/Codeunit_1.txt";

            _navisionObject = File.ReadAllText(original);
            _expectedObject = File.ReadAllText(result);
        };

        Because of = () =>
           _result = DynamicsNavFile.replaceDateTimeInString(new DateTime(2010, 1, 1, 12, 0, 0), _navisionObject);

        It should_replace_the_date = () => _result.ShouldEqual(_expectedObject);
    }
}
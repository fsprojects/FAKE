using System.IO;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Collections;

namespace Test.FAKECore.NAVFiles
{
    public class CanSplitObjectsToSeparateFiles
    {
        static string _navisionObject;
        static string _expectedTable3;
        static string _expectedTable4;
        static FSharpList<DynamicsNavFile.NavObject> _result;

        Establish context = () =>
        {
            const string table3 = @"NAVFiles/Table_3.txt";
            const string table4 = @"NAVFiles/Table_4.txt";
            const string original = @"NAVFiles/Table_3_and_4.txt";

            _navisionObject = File.ReadAllText(original);
            _expectedTable3 = File.ReadAllText(table3);
            _expectedTable4 = File.ReadAllText(table4);
        };

        Because of = () => _result = DynamicsNavFile.objectsInObjectString(_navisionObject);

        It should_find_two_objects = () => _result.Length.ShouldEqual(2);

        It should_find_first_object_type = () => _result[0].Type.ShouldEqual("Table");
        It should_find_first_object_id = () => _result[0].Id.ShouldEqual(3);
        It should_find_first_object_name = () => _result[0].Name.ShouldEqual("Payment Terms");
        It should_find_first_object_source = () => _result[0].Source.ShouldEqual(_expectedTable3);

        It should_find_second_object_type = () => _result[1].Type.ShouldEqual("Table");
        It should_find_second_object_id = () => _result[1].Id.ShouldEqual(4);
        It should_find_second_object_name = () => _result[1].Name.ShouldEqual("Currency");
        It should_find_second_object_source = () => _result[1].Source.ShouldEqual(_expectedTable4);
    }
}
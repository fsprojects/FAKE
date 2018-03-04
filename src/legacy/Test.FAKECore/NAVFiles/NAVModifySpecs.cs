using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.NAVFiles
{
    public class CanRemoveModifyFlag
    {
        static string _navisionObject;
        static string _expectedObject;

        Establish context = () =>
        {
            const string result = @"NAVFiles/Codeunit_1.txt";
            const string original = @"NAVFiles/Codeunit_1_Modified.txt";

            _navisionObject = File.ReadAllText(original);
            _expectedObject = File.ReadAllText(result);
        };

        It should_remove_the_modified_flag = () => DynamicsNavFile.removeModifiedFlag(_navisionObject)
            .ShouldEqual(_expectedObject);
    }
}
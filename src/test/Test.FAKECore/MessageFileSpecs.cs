using System;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.XMLHandling
{
    public class when_reading_the_message_file
    {
        static string _text;

        Because of = () => _text = StringHelper.ReadFileAsString("./MessageFiles/Message1.txt");

        It should_not_be_empty = () => _text.ShouldNotBeEmpty();
    }

    public class when_getting_the_test_suite
    {
        static DynamicsNav.TestResults _result;

        Because of = () => _result = DynamicsNav.analyzeTestResults("./MessageFiles/Message1.txt");

        It should_find_the_test_suite = () => _result.SuiteName.ShouldEqual("Test Math");
    }
}
using System;
using System.Linq;
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

    public class when_reading_message_file_without_testsuite
    {
        It should_not_find_a_suite = 
            () => DynamicsNav.analyzeTestResults("./MessageFiles/TestSuiteNotFound1.txt").ShouldBeNull();
    }

    public class when_reading_message_file_without_testsuite_but_with_traces
    {
        It should_not_find_a_suite =
            () => DynamicsNav.analyzeTestResults("./MessageFiles/TestSuiteNotFound2.txt").ShouldBeNull();
    }

    public class when_reading_message_file_with_skipped_testsuite
    {
        It should_not_find_a_suite =
            () => DynamicsNav.analyzeTestResults("./MessageFiles/SkippedTestSuite.txt").ShouldBeNull();
    }

    public class when_reading_message_file_with_finished_all_message
    {
        It should_not_find_a_suite =
            () => DynamicsNav.analyzeTestResults("./MessageFiles/FinishedAll.txt").ShouldBeNull();
    }

    public class when_getting_the_test_suite
    {
        static UnitTestHelper.TestResults _result;

        Because of = () => _result = DynamicsNav.analyzeTestResults("./MessageFiles/Message1.txt").Value;

        It should_find_the_test_suite = () => _result.SuiteName.ShouldEqual("Test Math");
        It should_find_all_tests = () => _result.Tests.Count().ShouldEqual(14);
    }

    public class when_getting_the_test_names
    {
        static UnitTestHelper.TestResults _result;

        Because of = () => _result = DynamicsNav.analyzeTestResults("./MessageFiles/Message1.txt").Value;

        It should_find_the_first_test = () =>
            _result.Tests[0].Name.ShouldEqual("TestGCD");

        It should_find_the_second_test = () => 
            _result.Tests[1].Name.ShouldEqual("TestLCM");

        It should_find_the_last_test = () =>
            _result.Tests.Last().Name.ShouldEqual("TestAddFractions2");
    }

    public class when_getting_the_test_runtimes
    {
        static UnitTestHelper.TestResults _result;

        Because of = () => _result = DynamicsNav.analyzeTestResults("./MessageFiles/Message1.txt").Value;

        It should_find_the_first_test = () => 
            _result.Tests[0].RunTime.ShouldEqual(TimeSpan.FromMilliseconds(218));

        It should_find_the_second_test = () =>
            _result.Tests[1].RunTime.ShouldEqual(TimeSpan.FromMilliseconds(8));

        It should_find_the_last_test = () => 
            _result.Tests.Last().RunTime.ShouldEqual(TimeSpan.FromMilliseconds(11));
    }

    public class when_getting_the_test_status
    {
        static UnitTestHelper.TestResults _result;

        Because of = () => _result = DynamicsNav.analyzeTestResults("./MessageFiles/Message1.txt").Value;

        It should_find_the_first_test = () =>
            _result.Tests[0].Status.ShouldEqual(UnitTestHelper.TestStatus.Ok);

        It should_find_the_second_test = () =>
            _result.Tests[1].Status.ShouldEqual(UnitTestHelper.TestStatus.Ok);

        It should_find_the_ignored_test = () =>
            _result.Tests[2].Status
            .ShouldEqual(UnitTestHelper.TestStatus.NewIgnored("Not implemented", "OK"));

        It should_find_last_error = () =>
            _result.Tests[12].Status
            .ShouldEqual(UnitTestHelper.TestStatus.NewFailure("Test failure", "Assert.IsTrue failed. %1"));
    }


    public class when_reading_the_second_message_file
    {
        static UnitTestHelper.TestResults _result;

        Because of = () => _result = DynamicsNav.analyzeTestResults("./MessageFiles/Message2.txt").Value;

        It should_find_the_first_test = () =>
            _result.Tests[0].Status.ShouldEqual(UnitTestHelper.TestStatus.Ok);

        It should_find_the_second_test = () =>
            _result.Tests[1].Status.ShouldEqual(UnitTestHelper.TestStatus.Ok);

        It should_find_last_error = () =>
            _result.Tests.Last().Status
            .ShouldEqual(UnitTestHelper.TestStatus.NewIgnored("", ""));
    }


    public class when_reading_the_third_message_file
    {
        static UnitTestHelper.TestResults _result;

        Because of = () => _result = DynamicsNav.analyzeTestResults("./MessageFiles/Message3.txt").Value;

        It should_find_the_runtime_in_the_first_test = () =>
            _result.Tests[0].RunTime.ShouldEqual(TimeSpan.FromMilliseconds(2));

        It should_find_the_runtime_in_the_last_test = () =>
            _result.Tests.Last().RunTime
            .ShouldEqual(TimeSpan.Zero);
    }

    public class when_reading_the_fourth_message_file
    {
        static UnitTestHelper.TestResults _result;

        Because of = () => _result = DynamicsNav.analyzeTestResults("./MessageFiles/Message4.txt").Value;

        It should_find_the_runtime_in_the_first_test = () =>
            _result.Tests[0].RunTime.ShouldEqual(TimeSpan.FromMilliseconds(2));

        It should_find_all_tests = () => _result.Tests.Count().ShouldEqual(3);
    }

    public class when_reading_the_minesweeper_message_file
    {
        static UnitTestHelper.TestResults _result;

        Because of = () => _result = DynamicsNav.analyzeTestResults("./MessageFiles/Message5.txt").Value;

        It should_find_the_runtime_in_the_first_test = () =>
            _result.Tests[0].RunTime.ShouldEqual(TimeSpan.FromMilliseconds(1));

        It should_find_all_tests = () => _result.Tests.Count().ShouldEqual(42);
    }

    public class when_reading_a_message_file_with_errors
    {
        static UnitTestHelper.TestResults _result;

        Because of = () => _result = DynamicsNav.analyzeTestResults("./MessageFiles/Message6.txt").Value;

        It should_parse_the_error = () =>
            _result.Tests[5].Status
            .ShouldEqual(UnitTestHelper.TestStatus.NewFailure("Test failure", "Error: Values are not equal. Expected: <aaaaaaaaaaaa...   ...[996]aaaaaaaaaaaaaaaaaaaaaaaaaaaaa> - Actual: <aaaaaaaaaaaa...   ...[996]aaaaa>"));

        It should_find_all_tests = () => _result.Tests.Count().ShouldEqual(8);
    }

    public class when_reading_the_math_message_file
    {
        static UnitTestHelper.TestResults _result;

        Because of = () => _result = DynamicsNav.analyzeTestResults("./MessageFiles/Message7.txt").Value;

        It should_parse_the_suite_name =
            () => _result.SuiteName.ShouldEqual("Test CodeCompression");

        It should_parse_the_first_test_name = 
            () =>_result.Tests[0].Name.ShouldEqual("BinaryAddition 1 (8 Bit + 8 Bit)");

        It should_find_all_tests = () => _result.Tests.Count().ShouldEqual(99);
    }
}
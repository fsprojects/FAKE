using System.IO;
using Fake;
using Machine.Specifications;
using System;
using System.Threading;
using System.Globalization;
using Microsoft.FSharp.Core;
using System.Linq;

namespace Test.FAKECore.NAVFiles
{
    public class CanParseTestResults
    {
        static FSharpOption<Fake.UnitTestHelper.TestResults> _results;

        Establish context = () =>
        {
            const string original = @"NAVFiles/TestResultsXMLPort130021.xml";
            _results = DynamicsNav.analyzeXmlTestResults(original, "FAKE");
        };
        
        It should_have_results = () => _results.ShouldNotBeNull();
        It should_have_testsuite_name = () => _results.Value.SuiteName.ShouldEqual("FAKE");
        It should_have_three_tests = () => _results.Value.Tests.Length.ShouldEqual(3);
        It should_have_two_successful_tests = () => _results.Value.Tests.Count(x => x.Status == Fake.UnitTestHelper.TestStatus.Ok).ShouldEqual(2);
    }
}
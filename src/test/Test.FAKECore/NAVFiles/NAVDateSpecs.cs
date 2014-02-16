using System.IO;
using Fake;
using Machine.Specifications;
using System;
using System.Threading;
using System.Globalization;

namespace Test.FAKECore.NAVFiles
{
    public class CanReplaceDate
    {
        static string _navisionObject;
        static string _expectedObject;
        static string _dateFormat;
        static string _result;

        Establish context = () =>
        {
            const string result = @"NAVFiles/Codeunit_1_with_Date_changed.txt";
            const string original = @"NAVFiles/Codeunit_1.txt";

            _navisionObject = File.ReadAllText(original);
            _expectedObject = File.ReadAllText(result);

            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            _dateFormat = Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("yyyy", "yy");
        };

        Because of = () =>
           _result = DynamicsNavFile.replaceDateTimeInStringWithFormat(new DateTime(2010, 1, 1, 12, 0, 0), _dateFormat, _navisionObject);

        It should_replace_the_date = () => _result.ShouldEqual(_expectedObject);
    }
}
using Fake;
using NUnit.Framework;

namespace Test.FAKECore.FileHandling
{
    public class BaseTest
    {
        [TearDown]
        public void WaitUntilEverythingIsPrinted()
        {
            TraceHelper.WaitUntilEverythingIsPrinted();
        }
    }
}
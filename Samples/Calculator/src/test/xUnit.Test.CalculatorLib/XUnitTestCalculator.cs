using System.Diagnostics;
using CalculatorLib;
using Xunit;

namespace xUnit.Test.CalculatorLib
{
    public class XUnitTestCalculator
    {
        [Fact]
        public static void TestAddWithXUnit()
        {
            Trace.WriteLine("TestAddWithXUnit");
            Assert.Equal(4, Calculator.Add(3, 1));
            Assert.Equal(0, Calculator.Add(0, 0));
        }
    }
}
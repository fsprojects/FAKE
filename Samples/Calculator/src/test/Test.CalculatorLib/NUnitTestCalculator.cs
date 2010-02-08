using System.Diagnostics;
using CalculatorLib;
using NUnit.Framework;

namespace NUnit.Test.CalculatorLib
{
    [TestFixture]
    public class NUnitTestCalculator
    {
        [Test]
        public static void TestAddWithNUnit()
        {
            Trace.WriteLine("TestAddWithNUnit");
            Assert.AreEqual(4, Calculator.Add(3, 1));
            Assert.AreEqual(0, Calculator.Add(0, 0));
        }
    }
}
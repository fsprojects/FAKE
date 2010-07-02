using System.Diagnostics;
using NUnit.Framework;

namespace CalculatorLib
{
    [TestFixture]
    public class CalculatorSpecs
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
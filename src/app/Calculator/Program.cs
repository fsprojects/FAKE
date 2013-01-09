using System;

namespace Calculator
{
    internal class Program
    {
        private static void Main()
        {
            int r = CalculatorLib.Calculator.Add(3, 4);
            Console.WriteLine("Result is {0}", r);
            Console.ReadLine();
        }
    }
}
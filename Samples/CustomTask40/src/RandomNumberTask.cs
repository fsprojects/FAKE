using System;

namespace MyCustomTask
{
    public class RandomNumberTask
    {
        public static int RandomNumber(int min, int max)
        {
            var random = new Random();
            return random.Next(min, max);
        }
    }
}
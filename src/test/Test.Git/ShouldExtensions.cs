using NUnit.Framework;

namespace Test.Git
{
    public static class ShouldExtensions
    {
        /// <summary>
        ///   Asserts that the item supplied by
        ///   <paramref name = "item" />
        ///   is equal to the
        ///   item supplied by
        ///   <paramref name = "expected" />
        ///   .
        /// </summary>
        public static T ShouldEqual<T>(this T item, T expected)
        {
            Assert.AreEqual(expected, item);
            return item;
        }
    }
}